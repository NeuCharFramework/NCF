/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatStreamController.cs
    文件功能描述：为桌面 Admin Chat 提供受 AdminOnly JWT 保护的流式消息接口

    创建标识：Senparc - 20260726

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

    修改标识：Senparc - 20260916
    修改描述：v0.9.0 增强 Admin Chat 取消与推理轨迹，并扩展 NeuBell WebHook 请求能力

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    修改标识：Senparc - 20261021
    修改描述：v0.11.1 流式输出携带工作区与会话标题

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 implement AdminChat memory storage and management system

    修改标识：Senparc - 20261105
    修改描述：v0.12.26 按会话隔离流式输出与回调

    修改标识：Senparc - 20261107
    修改描述：页面刷新或关闭后，AdminChat 任务继续执行并允许重新连接

----------------------------------------------------------------*/

using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.OHS.Local.Events;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

/// <summary>
/// Desktop Chat 专用流式接口。正文仍由 AdminOnly JWT 保护，EventBus 只负责完成后的变更通知。
/// </summary>
[ApiController]
[Route("api/Senparc.Areas.Admin/AdminChatStream")]
[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class AdminChatStreamController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AdminChatSessionService _sessionService;
    private readonly AdminChatMessageService _messageService;
    private readonly AdminChatAiService _chatAiService;
    private readonly AdminChatRunRegistry _runs;
    private readonly IStringLocalizer<AdminResource> _localizer;
    private readonly ILogger<AdminChatStreamController> _logger;
    private readonly IEventBus? _eventBus;
    private int _disconnectLogged;

    public AdminChatStreamController(
        AdminChatSessionService sessionService,
        AdminChatMessageService messageService,
        AdminChatAiService chatAiService,
        AdminChatRunRegistry runs,
        IServiceProvider serviceProvider,
        IStringLocalizer<AdminResource> localizer,
        ILogger<AdminChatStreamController> logger)
    {
        _sessionService = sessionService;
        _messageService = messageService;
        _chatAiService = chatAiService;
        _runs = runs;
        _localizer = localizer;
        _logger = logger;
        _eventBus = serviceProvider.GetService(typeof(IEventBus)) as IEventBus;
    }

    [HttpPost("send")]
    public async Task SendAsync(
        [FromBody] ChatMessageInputDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || request.SessionId <= 0
            || string.IsNullOrWhiteSpace(request.Content) && request.Attachments?.Count is not > 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = _localizer["AdminChat.StreamInputRequired"].Value }, cancellationToken);
            return;
        }

        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
        if (session == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = _localizer["AdminChat.StreamSessionForbidden"].Value }, cancellationToken);
            return;
        }

        var run = _runs.TryBegin(request.SessionId, userId);
        if (run == null)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = "该会话已有任务在执行。" }, CancellationToken.None);
            return;
        }

        var clientConnected = true;
        try
        {
        IReadOnlyList<AdminChatAttachment> attachments = Array.Empty<AdminChatAttachment>();
        var content = string.IsNullOrWhiteSpace(request.Content)
            ? AdminChatAttachmentService.AttachmentOnlyMessage
            : request.PromptReferences?.Count > 0 ? request.Content : request.Content.Trim();
        AdminChatMessage userMessage;
        try
        {
            if (request.Attachments?.Count > 0)
            {
                await HttpContext.RequestServices.GetRequiredService<AdminChatWorkspaceAssetService>()
                    .GetSessionAsync(request.SessionId, User, CancellationToken.None);
                attachments = await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                    .ValidateAsync(session, request.Attachments, CancellationToken.None);
            }
            userMessage = await _messageService.AddMessageAsync(
                request.SessionId, ChatMessageRoleType.User, content,
                attachments: attachments, promptReferences: request.PromptReferences);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or UnauthorizedAccessException or IOException or NotSupportedException or System.Xml.XmlException
            or Senparc.Ncf.Core.Exceptions.NcfExceptionBase)
        {
            Response.StatusCode = error is UnauthorizedAccessException ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = error.Message }, CancellationToken.None);
            return;
        }
        await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

        try
        {
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.ContentType = "text/event-stream";
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            clientConnected = await EmitAsync(run, "user-message", AdminChatMessageDto.CreateFromEntity(userMessage), clientConnected);
        }
        catch (Exception) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            clientConnected = false;
            NoteDisconnect(run);
        }

        var liveEvents = Channel.CreateUnbounded<AdminChatLiveEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        var generationTask = request.Mode == AdminChatMode.Harness
            ? GenerateHarnessResponseAsync(
                request.SessionId,
                userId,
                content,
                request.AiModelId,
                liveEvents.Writer,
                run.Token, attachments)
            : GenerateResponseAsync(
            request.SessionId,
            userId,
            content,
            request.AiModelId,
            liveEvents.Writer,
            run.Token, attachments);

        try
        {
            await foreach (var liveEvent in liveEvents.Reader.ReadAllAsync(run.Token).ConfigureAwait(false))
                clientConnected = await EmitLiveAsync(run, liveEvent, clientConnected).ConfigureAwait(false);

            var result = await generationTask.ConfigureAwait(false);
            var assistantMessage = await _messageService.AddMessageAsync(
                request.SessionId,
                ChatMessageRoleType.Assistant,
                result.Response,
                result.ModelIdentifier,
                result.Harness?.TrajectoryId,
                result.Harness?.TrajectorySequence);

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new AdminChatSyncEvent(userId, request.SessionId, "messages-changed"));
            }
            clientConnected = await EmitAsync(run, "assistant-message", AdminChatMessageDto.CreateFromEntity(assistantMessage), clientConnected);
            await EmitAsync(run, "done", new
            {
                sessionId = request.SessionId,
                memoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                memoryUpdateResult = _chatAiService.MemoryUpdateResult,
                memoryUsage = _chatAiService.MemoryUsage,
                mode = request.Mode,
                trajectoryId = result.Harness?.TrajectoryId ?? 0,
                trajectorySequence = result.Harness?.TrajectorySequence ?? 0,
                trajectoryStatus = result.Harness?.Status,
                pendingApprovals = result.Harness?.PendingApprovals
            }, clientConnected);
        }
        catch (OperationCanceledException) when (run.Token.IsCancellationRequested)
        {
            await ObserveGenerationAsync(generationTask).ConfigureAwait(false);
            _logger.LogInformation("AdminChat 任务已按用户请求停止。SessionId={SessionId}", request.SessionId);
            await EmitAsync(run, "cancelled", new { message = "本次请求已终止" }, clientConnected);
        }
        catch (Exception ex)
        {
            await ObserveGenerationAsync(generationTask).ConfigureAwait(false);
            _logger.LogError(ex, "AdminChat 任务执行失败。SessionId={SessionId}", request.SessionId);
            await EmitAsync(run, "error", new
            {
                message = string.IsNullOrWhiteSpace(ex.Message) ? _localizer["AdminChat.AgentReplyFailed"].Value : ex.Message
            }, clientConnected);
        }
        }
        finally
        {
            _runs.Finish(run);
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(int sessionId)
    {
        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0) return Unauthorized();
        if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null) return NotFound();
        var status = _runs.GetStatus(sessionId, userId);
        return Ok(new
        {
            running = status.Running,
            justFinished = status.JustFinished,
            text = status.Text,
            thinking = status.Thinking,
            status = status.Status
        });
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(int sessionId)
    {
        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0) return Unauthorized();
        if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null) return NotFound();
        var cancelled = _runs.Cancel(sessionId, userId);
        _logger.LogInformation("AdminChat 收到停止请求。SessionId={SessionId}, Cancelled={Cancelled}", sessionId, cancelled);
        return Ok(new { cancelled });
    }

    [HttpGet("attach")]
    public async Task Attach(int sessionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var run = _runs.Find(sessionId, userId);
        if (run == null)
        {
            Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        using var listener = _runs.Listen(run);
        try
        {
            await WriteRawEventAsync("resume", listener.SnapshotJson, cancellationToken).ConfigureAwait(false);
            await foreach (var frame in listener.Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var split = frame.IndexOf('\n');
                await WriteRawEventAsync(frame[..split], frame[(split + 1)..], cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("AdminChat 观察者已断开，任务继续执行。SessionId={SessionId}", sessionId);
        }
    }

    private async Task<bool> EmitLiveAsync(AdminChatRun run, AdminChatLiveEvent liveEvent, bool clientConnected)
    {
        switch (liveEvent.Kind)
        {
            case "token":
                return await EmitAsync(run, "token", new { text = liveEvent.Text }, clientConnected, state =>
                {
                    state.AssistantText += liveEvent.Text ?? "";
                    state.ThinkingText = "";
                    state.StatusText = "正在生成回答…";
                });
            case "usage":
                return await EmitAsync(run, "usage", liveEvent.Usage, clientConnected);
            case "assistant-phase":
                return await EmitAsync(run, "assistant-phase", liveEvent, clientConnected, state =>
                {
                    var reasoning = liveEvent.TrajectoryEvent?.EventType == "TextReasoningContent";
                    if (reasoning)
                    {
                        state.ThinkingText = liveEvent.Text ?? liveEvent.TrajectoryEvent?.Content ?? "";
                        state.StatusText = "正在思考…";
                    }
                    else if (!string.IsNullOrEmpty(liveEvent.Text))
                    {
                        state.AssistantText = liveEvent.Text;
                        state.StatusText = "正在生成回答…";
                    }
                });
            case "trajectory-event":
                return await EmitAsync(run, "trajectory-event", liveEvent, clientConnected,
                    state => state.StatusText = "正在处理任务…");
            case "trajectory-complete":
                return await EmitAsync(run, "trajectory-complete", liveEvent, clientConnected, state =>
                    state.StatusText = liveEvent.PendingApprovals?.Count > 0 ? "等待你的确认…" : "任务已完成，正在保存…");
            default:
                return clientConnected;
        }
    }

    private async Task<bool> EmitAsync(
        AdminChatRun run, string eventName, object payload, bool clientConnected, Action<AdminChatRun> mutate = null)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        _runs.Publish(run, eventName, json, mutate);
        if (!clientConnected || HttpContext.RequestAborted.IsCancellationRequested)
        {
            NoteDisconnect(run);
            return false;
        }

        try
        {
            await WriteRawEventAsync(eventName, json, CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or ObjectDisposedException)
        {
            NoteDisconnect(run);
            return false;
        }
    }

    private async Task WriteRawEventAsync(string eventName, string json, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken).ConfigureAwait(false);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void NoteDisconnect(AdminChatRun run)
    {
        if (Interlocked.Exchange(ref _disconnectLogged, 1) == 0)
            _logger.LogInformation("AdminChat 页面已断开，任务继续执行。SessionId={SessionId}", run.SessionId);
    }

    private static async Task ObserveGenerationAsync(Task generationTask)
    {
        try { await generationTask.ConfigureAwait(false); }
        catch (Exception)
        {
            // 生成失败或停止已经由调用方发布给仍在连接的页面。
        }
    }

    private async Task<AdminChatStreamResult> GenerateResponseAsync(
        int sessionId,
        int userId,
        string content,
        int aiModelId,
        ChannelWriter<AdminChatLiveEvent> writer,
        CancellationToken cancellationToken,
        IReadOnlyList<AdminChatAttachment> attachments)
        {
            try
            {
                var result = await _chatAiService.GenerateResponseAsync(
                    sessionId,
                    userId,
                    content,
                    aiModelId,
                    chunk => writer.TryWrite(new AdminChatLiveEvent
                    {
                        Kind = "token",
                        Text = chunk
                    }),
                    cancellationToken: cancellationToken,
                    onUsage: usage => writer.TryWrite(new AdminChatLiveEvent
                    {
                        Kind = "usage",
                        Usage = usage
                    }), attachments: attachments);
                return new AdminChatStreamResult
                {
                    Response = result.response,
                    ModelIdentifier = result.modelIdentifier
                };
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
                throw;
            }
            finally
            {
                writer.TryComplete();
            }
        }

    private async Task<AdminChatStreamResult> GenerateHarnessResponseAsync(
        int sessionId,
        int userId,
        string content,
        int aiModelId,
        ChannelWriter<AdminChatLiveEvent> writer,
        CancellationToken cancellationToken,
        IReadOnlyList<AdminChatAttachment> attachments)
    {
        try
        {
            var result = await _chatAiService.GenerateNativeHarnessResponseAsync(
                sessionId,
                userId,
                content,
                aiModelId,
                cancellationToken: cancellationToken,
                onLiveEvent: liveEvent => writer.TryWrite(liveEvent), attachments: attachments);
            return new AdminChatStreamResult
            {
                Response = result.response,
                ModelIdentifier = result.modelIdentifier,
                Harness = result.harness
            };
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task WriteEventAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(payload, JsonOptions);
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken).ConfigureAwait(false);
        await Response.WriteAsync($"data: {serialized}\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed class AdminChatStreamResult
    {
        public string Response { get; set; }
        public string ModelIdentifier { get; set; }
        public AdminChatHarnessResult Harness { get; set; }
    }

    private int GetCurrentAdminUserInfoId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : -1;
    }
}
