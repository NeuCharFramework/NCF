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
    private readonly IStringLocalizer<AdminResource> _localizer;
    private readonly IEventBus? _eventBus;

    public AdminChatStreamController(
        AdminChatSessionService sessionService,
        AdminChatMessageService messageService,
        AdminChatAiService chatAiService,
        IServiceProvider serviceProvider,
        IStringLocalizer<AdminResource> localizer)
    {
        _sessionService = sessionService;
        _messageService = messageService;
        _chatAiService = chatAiService;
        _localizer = localizer;
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

        IReadOnlyList<AdminChatAttachment> attachments = Array.Empty<AdminChatAttachment>();
        try
        {
            if (request.Attachments?.Count > 0)
            {
                await HttpContext.RequestServices.GetRequiredService<AdminChatWorkspaceAssetService>()
                    .GetSessionAsync(request.SessionId, User, cancellationToken);
                attachments = await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                    .ValidateAsync(session, request.Attachments, cancellationToken);
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or UnauthorizedAccessException or IOException or NotSupportedException or System.Xml.XmlException)
        {
            Response.StatusCode = error is UnauthorizedAccessException ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = error.Message }, cancellationToken);
            return;
        }
        var content = string.IsNullOrWhiteSpace(request.Content)
            ? AdminChatAttachmentService.AttachmentOnlyMessage : request.Content.Trim();
        var userMessage = await _messageService.AddMessageAsync(
            request.SessionId,
            ChatMessageRoleType.User,
            content, attachments: attachments);
        await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        await WriteEventAsync("user-message", AdminChatMessageDto.CreateFromEntity(userMessage), cancellationToken);

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
                cancellationToken, attachments)
            : GenerateResponseAsync(
            request.SessionId,
            userId,
            content,
            request.AiModelId,
            liveEvents.Writer,
            cancellationToken, attachments);

        try
        {
            await foreach (var liveEvent in liveEvents.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                switch (liveEvent.Kind)
                {
                    case "token":
                        await WriteEventAsync("token", new { text = liveEvent.Text }, cancellationToken);
                        break;
                    case "usage":
                        await WriteEventAsync("usage", liveEvent.Usage, cancellationToken);
                        break;
                    case "assistant-phase":
                        await WriteEventAsync("assistant-phase", liveEvent, cancellationToken);
                        break;
                    case "trajectory-event":
                        await WriteEventAsync("trajectory-event", liveEvent, cancellationToken);
                        break;
                    case "trajectory-complete":
                        await WriteEventAsync("trajectory-complete", liveEvent, cancellationToken);
                        break;
                }
            }

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
            await WriteEventAsync("assistant-message", AdminChatMessageDto.CreateFromEntity(assistantMessage), cancellationToken);
            await WriteEventAsync("done", new
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
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 客户端主动断开时结束本次 HTTP 流，不输出异常堆栈，也不影响站点其它请求。
        }
        catch (Exception ex)
        {
            try
            {
                await WriteEventAsync(
                    "error",
                    new { message = string.IsNullOrWhiteSpace(ex.Message) ? _localizer["AdminChat.AgentReplyFailed"].Value : ex.Message },
                    CancellationToken.None);
            }
            catch
            {
                // 响应连接已断开时无需再次抛出写入异常。
            }
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
