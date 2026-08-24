/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ChatAgentService.cs
    文件功能描述：基于 EventBus 的系统级 ChatAgent 与 NeuCharPivot 生成服务


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Service;
using Senparc.Ncf.Shared.Abstractions.ChatAgent;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class ChatAgentRequestEventHandler : IIntegrationEventHandler<ChatAgentRequestEvent>
{
    private readonly ChatAgentNeuCharPivotComposer _composer;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ChatAgentRequestEventHandler> _logger;

    public ChatAgentRequestEventHandler(
        ChatAgentNeuCharPivotComposer composer,
        IEventBus eventBus,
        ILogger<ChatAgentRequestEventHandler> logger)
    {
        _composer = composer;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(ChatAgentRequestEvent @event, CancellationToken cancellationToken)
    {
        ChatAgentResponseEvent response;
        try
        {
            response = await _composer.ComposeAsync(@event, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ChatAgent 处理失败：RequestId={RequestId}, Target={TargetModuleUid}",
                @event.RequestId,
                @event.TargetModuleUid);
            response = new ChatAgentResponseEvent(
                @event.RequestId,
                false,
                @event.CurrentSchemaJson,
                @event.ChatSessionId,
                "NeuCharPivot 生成失败。",
                ex.Message);
        }

        await _eventBus.PublishDerivedAsync(response, @event, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ChatAgentNeuCharPivotComposer
{
    private readonly NeuCharPivotService _pivotService;
    private readonly NeuCharFunctionService _functionService;
    private readonly NeuCharPivotConfigurationService _configurationService;
    private readonly NeuCharExecutionLogService _executionLogService;
    private readonly AdminChatSessionService _sessionService;
    private readonly AdminChatMessageService _messageService;
    private readonly AdminChatSessionModuleService _sessionModuleService;
    private readonly AdminChatAiService _aiService;
    private readonly XncfModuleService _moduleService;
    private readonly ILogger<ChatAgentNeuCharPivotComposer> _logger;

    public ChatAgentNeuCharPivotComposer(
        NeuCharPivotService pivotService,
        NeuCharFunctionService functionService,
        NeuCharPivotConfigurationService configurationService,
        NeuCharExecutionLogService executionLogService,
        AdminChatSessionService sessionService,
        AdminChatMessageService messageService,
        AdminChatSessionModuleService sessionModuleService,
        AdminChatAiService aiService,
        XncfModuleService moduleService,
        ILogger<ChatAgentNeuCharPivotComposer> logger)
    {
        _pivotService = pivotService;
        _functionService = functionService;
        _configurationService = configurationService;
        _executionLogService = executionLogService;
        _sessionService = sessionService;
        _messageService = messageService;
        _sessionModuleService = sessionModuleService;
        _aiService = aiService;
        _moduleService = moduleService;
        _logger = logger;
    }

    public async Task<ChatAgentResponseEvent> ComposeAsync(
        ChatAgentRequestEvent request,
        CancellationToken cancellationToken)
    {
        if (request.AdminUserId <= 0)
        {
            return Failure(request, "ChatAgent 的持久化生成操作必须由已登录管理员发起。");
        }
        if (!Enum.IsDefined(request.Operation))
        {
            return Failure(request, "ChatAgent 操作不在公开白名单中。");
        }
        if (request.RequestId == Guid.Empty || request.UserRequirement?.Length > 10_000 ||
            request.CurrentSchemaJson?.Length > 500_000)
        {
            return Failure(request, "ChatAgent 请求内容超过允许的长度。");
        }

        var callerRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z =>
            string.Equals(z.Uid, request.CallerModuleUid, StringComparison.OrdinalIgnoreCase));
        var callerModule = callerRegister == null
            ? null
            : await _moduleService.GetObjectAsync(z => z.Uid == callerRegister.Uid).ConfigureAwait(false);
        if (callerRegister == null || callerModule?.State != XncfModules_State.开放)
        {
            return Failure(request, "调用方模块未安装、未加载或未开启，ChatAgent 已拒绝请求。");
        }
        if (!string.Equals(callerRegister.Uid, Register.ModuleUid, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(callerRegister.Uid, request.TargetModuleUid, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(request, "扩展模块只能请求自身的 ChatAgent 能力；跨模块操作仅允许系统管理模块发起。");
        }

        var targetRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z =>
            string.Equals(z.Uid, request.TargetModuleUid, StringComparison.OrdinalIgnoreCase));
        var targetModule = targetRegister == null
            ? null
            : await _moduleService.GetObjectAsync(z => z.Uid == targetRegister.Uid).ConfigureAwait(false);
        if (targetRegister == null || targetModule?.State != XncfModules_State.开放)
        {
            return Failure(request, "目标模块未安装、未加载或未开启，不能生成 NeuCharPivot。");
        }

        var catalog = await _functionService.GetCatalogAsync(
            request.TargetModuleUid,
            true,
            cancellationToken).ConfigureAwait(false);
        if (catalog.Count == 0)
        {
            return Failure(request, "目标模块没有已注册的 Function。");
        }

        var log = new NeuCharExecutionLog(
            "chat-agent",
            0,
            request.TargetModuleUid,
            null,
            request.Operation.ToString(),
            request.RequestId.ToString("N"));
        await _executionLogService.SaveObjectAsync(log).ConfigureAwait(false);

        var sessionId = request.ChatSessionId;
        try
        {
            var current = await _pivotService.GetSnapshotAsync(request.TargetModuleUid, cancellationToken)
                .ConfigureAwait(false);
            sessionId ??= current?.Configuration.ChatSessionId;
            if (sessionId.HasValue)
            {
                var ownedSession = await _sessionService.GetSessionByIdAsync(sessionId.Value, request.AdminUserId)
                    .ConfigureAwait(false);
                if (ownedSession == null)
                {
                    sessionId = null;
                }
            }

            if (!sessionId.HasValue)
            {
                var session = await _sessionService.CreateSessionAsync(
                    $"NeuCharPivot · {targetRegister.MenuName}",
                    request.AdminUserId).ConfigureAwait(false);
                sessionId = session.Id;
                await _sessionModuleService.AddModulesToSessionAsync(
                    session.Id,
                    new List<(string uid, string name, string version)>
                    {
                        (targetRegister.Uid, targetRegister.MenuName, targetRegister.Version)
                    })
                    .ConfigureAwait(false);
            }

            var requirement = string.IsNullOrWhiteSpace(request.UserRequirement)
                ? "Generate a concise and clean operation panel for all Functions."
                : request.UserRequirement.Trim();
            var prompt = BuildPrompt(request, requirement, catalog);
            await _messageService.AddMessageAsync(
                sessionId.Value,
                ChatMessageRoleType.User,
                requirement).ConfigureAwait(false);

            string candidateJson;
            string modelIdentifier;
            Exception generationException = null;
            try
            {
                (candidateJson, modelIdentifier) = await _aiService.GenerateResponseAsync(
                    sessionId.Value,
                    request.AdminUserId,
                    prompt,
                    request.AiModelId,
                    null,
                    new AdminChatGenerationOptions
                    {
                        SystemInstructions = BuildSystemInstructions(),
                        AllowFunctionInvocation = false,
                        MaxOutputTokens = 5000,
                        Temperature = 0.2f
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                generationException = ex;
                _logger.LogWarning(ex,
                    "NeuCharPivot AI 生成失败，使用完整 Function 目录回退：ModuleUid={ModuleUid}",
                    request.TargetModuleUid);
                candidateJson = _pivotService.BuildFallbackSchemaJson(catalog);
                modelIdentifier = "deterministic-fallback";
            }

            var snapshot = await _pivotService.SaveGeneratedLayoutAsync(
                request.TargetModuleUid,
                requirement,
                request.AiModelId,
                request.AdminUserId,
                sessionId,
                candidateJson,
                cancellationToken).ConfigureAwait(false);
            await _messageService.AddMessageAsync(
                sessionId.Value,
                ChatMessageRoleType.Assistant,
                snapshot.Configuration.LayoutSchemaJson,
                modelIdentifier).ConfigureAwait(false);

            if (generationException != null)
            {
                snapshot.Configuration.RecordError(
                    $"AI 生成失败，已使用确定性 Function 目录回退：{generationException.Message}");
                await _configurationService.SaveObjectAsync(snapshot.Configuration).ConfigureAwait(false);
            }

            log.Complete(
                generationException == null,
                $"Revision={snapshot.Configuration.Revision}; Model={modelIdentifier}",
                generationException?.ToString());
            await _executionLogService.SaveObjectAsync(log).ConfigureAwait(false);
            return new ChatAgentResponseEvent(
                request.RequestId,
                true,
                snapshot.Configuration.LayoutSchemaJson,
                sessionId,
                generationException == null
                    ? "NeuCharPivot 已生成并保存。"
                    : "AI 暂时不可用，已保存确定性回退面板；异常已记录。");
        }
        catch (Exception ex)
        {
            log.Complete(false, null, ex.ToString());
            await _executionLogService.SaveObjectAsync(log).ConfigureAwait(false);
            var configuration = await _configurationService.GetObjectAsync(z =>
                z.ModuleUid == request.TargetModuleUid).ConfigureAwait(false);
            if (configuration != null)
            {
                configuration.RecordError(ex.Message);
                await _configurationService.SaveObjectAsync(configuration).ConfigureAwait(false);
            }
            throw;
        }
    }

    private static string BuildSystemInstructions() =>
        """
        You are NeuCharFramework's system-level ChatAgent. Generate only a declarative NeuCharPivot JSON object.
        Never emit HTML, JavaScript, CSS, URLs, executable code, tool calls, method calls, secrets, or markdown fences.
        You may only arrange the supplied Function keys and choose which supplied parameter names are exposed.
        Required parameters must always be exposed. Keep the UI concise, clean, beginner-friendly, and task-oriented.
        Return JSON with: version, title, description, columns (1-3), sections[] { title, functions[] { functionKey, title, summary, accent, exposedParameters[] } }.
        Allowed accent values: blue, green, orange, purple, gray.
        """;

    private static string BuildPrompt(
        ChatAgentRequestEvent request,
        string requirement,
        System.Collections.Generic.IReadOnlyList<NeuCharFunctionDescriptor> catalog)
    {
        var safeCatalog = catalog.Select(function => new
        {
            function.FunctionKey,
            function.Name,
            function.Description,
            Parameters = function.Parameters.Select(parameter => new
            {
                parameter.Name,
                parameter.Title,
                parameter.Description,
                parameter.IsRequired,
                ParameterType = parameter.ParameterType.ToString(),
                Options = parameter.SelectionList?.Items?.Select(option => new
                {
                    option.Value,
                    option.Text,
                    option.Note
                })
            })
        });

        var builder = new StringBuilder();
        builder.AppendLine(request.Operation == ChatAgentOperation.RefineNeuCharPivot
            ? "Refine the current NeuCharPivot schema according to the user's new requirement."
            : "Create a new NeuCharPivot schema that includes every supplied Function exactly once.");
        builder.AppendLine($"User requirement: {requirement}");
        if (!string.IsNullOrWhiteSpace(request.CurrentSchemaJson))
        {
            builder.AppendLine("Current normalized schema:");
            builder.AppendLine(request.CurrentSchemaJson);
        }
        builder.AppendLine("Available public Function metadata:");
        builder.AppendLine(JsonSerializer.Serialize(safeCatalog));
        return builder.ToString();
    }

    private static ChatAgentResponseEvent Failure(ChatAgentRequestEvent request, string error) =>
        new(request.RequestId, false, request.CurrentSchemaJson, request.ChatSessionId, error, error);
}
