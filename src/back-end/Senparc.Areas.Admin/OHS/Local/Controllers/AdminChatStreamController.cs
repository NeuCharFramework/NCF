/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatStreamController.cs
    文件功能描述：为桌面 Admin Chat 提供受 AdminOnly JWT 保护的流式消息接口

    创建标识：Senparc - 20260726

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
        if (request == null || request.SessionId <= 0 || string.IsNullOrWhiteSpace(request.Content))
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

        var content = request.Content.Trim();
        var userMessage = await _messageService.AddMessageAsync(
            request.SessionId,
            ChatMessageRoleType.User,
            content);
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
                cancellationToken)
            : GenerateResponseAsync(
            request.SessionId,
            userId,
            content,
            request.AiModelId,
            liveEvents.Writer,
            cancellationToken);

        try
        {
            await foreach (var liveEvent in liveEvents.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                switch (liveEvent.Kind)
                {
                    case "token":
                        await WriteEventAsync("token", new { text = liveEvent.Text }, cancellationToken);
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
        CancellationToken cancellationToken)
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
                    cancellationToken: cancellationToken);
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
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _chatAiService.GenerateNativeHarnessResponseAsync(
                sessionId,
                userId,
                content,
                aiModelId,
                cancellationToken: cancellationToken,
                onLiveEvent: liveEvent => writer.TryWrite(liveEvent));
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
