/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminChatAiService.cs
    文件功能描述：AdminChatAiService 服务逻辑


    创建标识：Senparc - 20260327

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强后台模块批量更新并完善多语言管理界面

    修改标识：Senparc - 20260731
    修改描述：v0.2.1 切换到新版 AgentKernel 原生 RunChatAsync 接口以适配 CO2NET 4.2.0

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

    修改标识：Senparc - 20260815
    修改描述：v0.5.1 优化管理端 AI 插件与知识库交互

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

    修改标识：Senparc - 20260916
    修改描述：v0.9.0 增强 Admin Chat 取消与推理轨迹，并扩展 NeuBell WebHook 请求能力

    修改标识：Senparc - 20260917
    修改描述：v0.10.0 增强 Admin Chat FunctionRender 参数元数据与工具调用兼容

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Harness;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services.AIPlugins;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.AI;
using Senparc.AI.AgentKernel.IWantToExtensions;
using Senparc.AI.AgentKernel.Extensions;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using MafFunctionCallContent = Microsoft.Extensions.AI.FunctionCallContent;
using MafFunctionResultContent = Microsoft.Extensions.AI.FunctionResultContent;
using MafTextContent = Microsoft.Extensions.AI.TextContent;
using MafTextReasoningContent = Microsoft.Extensions.AI.TextReasoningContent;
using MafToolApprovalRequestContent = Microsoft.Extensions.AI.ToolApprovalRequestContent;
using MafToolApprovalResponseContent = Microsoft.Extensions.AI.ToolApprovalResponseContent;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 系统级 ChatAgent 调用选项。普通 AdminChat 保持既有体验，只能加载模块显式声明的
    /// FunctionRender 白名单；系统生成场景可显式关闭全部 Function 工具。
    /// </summary>
    public sealed class AdminChatGenerationOptions
    {
        public string SystemInstructions { get; init; }
        public bool? AllowFunctionInvocation { get; init; }
        public int MaxOutputTokens { get; init; } = 2000;
        public float Temperature { get; init; } = 0.6f;
    }

    /// <summary>
    /// AdminChatAiService：管理后台聊天 AI 调用服务。
    /// 默认使用 appsettings 中的 SenparcAiSetting，也支持按请求切换到 AIKernel 中配置的 Chat 模型。
    /// </summary>
    public class AdminChatAiService
    {
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;
        private readonly AdminChatSessionWorkflowService _sessionWorkflowService;
        private readonly AdminChatTrajectoryService _trajectoryService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AdminChatAiService> _logger;

        /// <summary>
        /// 初始化管理后台聊天 AI 服务。
        /// </summary>
        /// <param name="messageService">聊天消息服务。</param>
        /// <param name="sessionModuleService">会话模块服务。</param>
        /// <param name="serviceProvider">服务提供器。</param>
        /// <param name="logger">日志记录器。</param>
        public AdminChatAiService(
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService,
            AdminChatSessionWorkflowService sessionWorkflowService,
            AdminChatTrajectoryService trajectoryService,
            IServiceProvider serviceProvider,
            ILogger<AdminChatAiService> logger)
        {
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
            _sessionWorkflowService = sessionWorkflowService;
            _trajectoryService = trajectoryService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// 生成 AI 回复内容并返回所使用的模型标识。
        /// </summary>
        /// <param name="sessionId">会话 Id。</param>
        /// <param name="userId">用户 Id。</param>
        /// <param name="userMessage">用户输入消息。</param>
        /// <param name="aiModelId">可选 AIModelId，0 表示系统级 SenparcAiSetting。</param>
        /// <returns>返回回复文本与模型标识。</returns>
        public async Task<(string response, string modelIdentifier)> GenerateResponseAsync(
            int sessionId,
            int userId,
            string userMessage,
            int aiModelId = 0,
            Action<string> onChunk = null,
            AdminChatGenerationOptions generationOptions = null,
            CancellationToken cancellationToken = default)
        {
            var showLoadedFunctionsInConsole = true;//是否输出 function 的 schema 信息到控制台，便于调试和验证 Function Calling 功能是否正确加载了函数

            var (setting, modelIdentifier) = await ResolveChatSettingAsync(aiModelId);

            var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
            var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
            var workflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(sessionId);

            var agentAiHandler = new AgentAiHandler(setting);


            var functionInvocationEnabled = generationOptions?.AllowFunctionInvocation != false;
            var build = await BuildAdminChatFunctionsAsync(setting, agentAiHandler, sessionId, userId, modules, workflows, functionInvocationEnabled);
            var aiFunctions = build.Functions;
            var moduleUids = modules.Where(z => !z.XncfModuleUid.IsNullOrEmpty()).Select(z => z.XncfModuleUid).ToList();
            var importedFunctionCount = build.ImportedFunctionCount;
            var importedWorkflowCount = build.ImportedWorkflowCount;
            var importedPluginNames = build.PluginNames;
            var importedFunctionSignatures = build.Signatures;
            var loadedFunctionDebugLines = build.DebugLines;


#pragma warning disable MEAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            var iWantToRun = await agentAiHandler.IWantTo(setting).ConfigChatModel($"AdminChat-{userId}-{sessionId}", new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Instructions = generationOptions?.SystemInstructions ?? BuildSystemMessage(modules),
                    MaxOutputTokens = Math.Clamp(generationOptions?.MaxOutputTokens ?? 2000, 256, 8000),
                    TopP = 0.9f,
                    Temperature = Math.Clamp(generationOptions?.Temperature ?? 0.6f, 0f, 1.5f),
                    AllowMultipleToolCalls = aiFunctions.Count > 0,
                    Tools = aiFunctions.Count > 0 ? aiFunctions.Cast<AITool>().ToList() : null
                },
                ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
                {
                    ChatReducer = new MessageCountingChatReducer(20)
                })
            }
                ).BuildKernelWithAgentSessionAsync();
#pragma warning restore MEAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。


            if (importedFunctionCount == 0)
            {
                _logger.LogWarning(
                    "AdminChat 未注入任何 FunctionRender 函数：SessionId={SessionId}, UserId={UserId}, ModuleCount={ModuleCount}, Modules={Modules}, SelectedWorkflowCount={WorkflowCount}",
                    sessionId,
                    userId,
                    moduleUids.Count,
                    string.Join(",", moduleUids),
                    importedWorkflowCount);
            }
            else
            {
                if (showLoadedFunctionsInConsole)
                {
                    WriteLoadedFunctionsToConsole(sessionId, userId, loadedFunctionDebugLines);
                }
            }

            _logger.LogInformation(
                "AdminChat FunctionCalling 插件加载完成：SessionId={SessionId}, UserId={UserId}, ModuleCount={ModuleCount}, Modules={Modules}, Plugins={Plugins}, Functions={Functions}, Workflows={Workflows}, FunctionList={FunctionList}",
                sessionId,
                userId,
                moduleUids.Count,
                string.Join(",", moduleUids),
                string.Join(",", importedPluginNames),
                importedFunctionCount,
                importedWorkflowCount,
                string.Join(" | ", importedFunctionSignatures));

            var prompt = BuildUserPrompt(messages, userMessage);

            // 使用 FunctionChoiceBehavior.Auto() 让 AI 根据需要自动调用 ModuleAssistantPlugin 函数
            //var executionSettings = new PromptExecutionSettings
            //{
            //    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            //};

            // TODO: 测试 MAF 中是否自动开启工具调用
            // 当调用方提供回调时，使用 AgentKernel 已有的流式回调；没有回调时保留原有整包路径。
            var hasStreamedChunk = false;
            string responseText;
            if (onChunk == null)
            {
                var skResult = await iWantToRun.RunChatAsync(prompt);
                responseText = skResult?.OutputString;
            }
            else
            {
                responseText = await ExecuteRunnerWithSessionRetryAsync(
                    iWantToRun,
                    prompt,
                    update =>
                    {
                        var updateText = update?.Text;
                        if (string.IsNullOrEmpty(updateText))
                        {
                            return;
                        }

                        hasStreamedChunk = true;
                        onChunk(updateText);
                    },
                    cancellationToken);
            }

            var result = responseText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("AI 返回空内容：SessionId={SessionId}, UserId={UserId}", sessionId, userId);
                result = "抱歉，我暂时没有生成有效回复，请稍后再试。";
            }

            // 某些模型/网关不提供增量内容，但仍返回最终文本。为流式客户端补发一个完整片段，
            // 这样界面不会一直停留在“正在回复”状态。
            if (onChunk != null && !hasStreamedChunk)
            {
                onChunk(result);
            }

            return (result, modelIdentifier);
        }

        /// <summary>
        /// Compatibility wrapper for the former prompt-marker Harness implementation.
        /// </summary>
        [Obsolete("Use GenerateNativeHarnessResponseAsync for the native Microsoft Agent Framework Harness.")]
        public async Task<(string response, string modelIdentifier, AdminChatHarnessResult harness)> GenerateHarnessResponseAsync(
            int sessionId,
            int userId,
            string userMessage,
            int aiModelId = 0,
            Action<AdminChatHarnessStep> onStep = null,
            int maxSteps = 0,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var (setting, modelIdentifier) = await ResolveChatSettingAsync(aiModelId);

            var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
            var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
            var workflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(sessionId);

            var agentAiHandler = new AgentAiHandler(setting);
            var build = await BuildAdminChatFunctionsAsync(setting, agentAiHandler, sessionId, userId, modules, workflows, functionInvocationEnabled: true);

            var effectiveMaxSteps = maxSteps > 0 ? maxSteps : 8;
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(120);

#pragma warning disable MEAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            var iWantToRun = await agentAiHandler.IWantTo(setting).ConfigChatModel($"AdminChatHarness-{userId}-{sessionId}", new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Instructions = BuildSystemMessage(modules) + AdminChatHarnessExecutor.BuildProtocolInstructions(),
                    MaxOutputTokens = 4000,
                    TopP = 0.9f,
                    Temperature = 0.4f,
                    AllowMultipleToolCalls = build.Functions.Count > 0,
                    Tools = build.Functions.Count > 0 ? build.Functions.Cast<AITool>().ToList() : null
                },
                ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
                {
                    ChatReducer = new MessageCountingChatReducer(20)
                })
            }
                ).BuildKernelWithAgentSessionAsync();
#pragma warning restore MEAI001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。

            var firstTurnPrompt = BuildUserPrompt(messages, userMessage) + AdminChatHarnessExecutor.BuildStartTail();
            var continuePrompt = AdminChatHarnessExecutor.BuildContinuePrompt();

            var executor = new AdminChatHarnessExecutor
            {
                MaxSteps = effectiveMaxSteps,
                Timeout = effectiveTimeout,
                TurnExecutor = async (step, prompt, ct) =>
                {
                    var session = iWantToRun.Kernel?.AgentSession;
                    var text = await ExecuteHarnessTurnAsync(iWantToRun, session, prompt, ct);
                    return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
                }
            };

            var result = await executor.RunAsync(firstTurnPrompt, continuePrompt, onStep, cancellationToken);
            return (result.FinalText, modelIdentifier, result);
        }

        private static async Task<string> ExecuteHarnessTurnAsync(
            IWantToRun runner,
            AgentSession session,
            string prompt,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await runner.RunChatResponseAsync(prompt, session, options: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                return response?.Text?.Trim() ?? string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch when (session != null)
            {
                var response = await runner.RunChatResponseAsync(prompt, agentSession: null, options: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                return response?.Text?.Trim() ?? string.Empty;
            }
        }

        /// <summary>
        /// Native Microsoft Agent Framework Harness execution with durable Trajectory events.
        /// </summary>
        public async Task<(string response, string modelIdentifier, AdminChatHarnessResult harness)> GenerateNativeHarnessResponseAsync(
            int sessionId,
            int userId,
            string userMessage,
            int aiModelId = 0,
            int maxIterations = 32,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default,
            Action<AdminChatLiveEvent> onLiveEvent = null)
        {
            var (setting, modelIdentifier) = await ResolveChatSettingAsync(aiModelId);
            var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
            var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
            var workflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(sessionId);
            var agentAiHandler = new AgentAiHandler(setting);
            var build = await BuildAdminChatFunctionsAsync(setting, agentAiHandler, sessionId, userId, modules, workflows, true);
            var trajectory = await _trajectoryService.CreateAsync(
                sessionId,
                userId,
                userMessage,
                AdminChatMode.Harness,
                modelIdentifier);

            await _trajectoryService.AppendAsync(
                trajectory,
                "request",
                "user",
                "message",
                userMessage,
                JsonSerializer.Serialize(new { sessionId, userId, aiModelId }));

            var iWantToRun = agentAiHandler.IWantTo(setting)
                .ConfigChatModel($"AdminChatHarness-{userId}-{sessionId}", new ChatClientAgentOptions())
                .BuildKernel();

            var harnessAgent = await iWantToRun.BuildHarnessAgentAsync(
                new AgentKernelHarnessOptions
                {
                    MaxContextWindowTokens = 32_768,
                    MaxOutputTokens = 4_000,
                    HarnessOptions = new Microsoft.Agents.AI.HarnessAgentOptions
                    {
                        Name = $"AdminChatHarness-{sessionId}",
                        Description = "NeuCharFramework Admin Chat long-running task agent",
                        ChatOptions = BuildHarnessChatOptions(
                            modules,
                            build.Functions),
                        DisableFileAccess = true,
                        DisableFileMemory = true,
                        DisableAgentSkillsProvider = true,
                        DisableWebSearch = true,
                        MaximumIterationsPerRequest = Math.Max(1, maxIterations)
                    }
                },
                cancellationToken: cancellationToken);

            var result = await ExecuteNativeHarnessAsync(
                trajectory,
                harnessAgent,
                BuildUserPrompt(messages, userMessage),
                timeout ?? TimeSpan.FromMinutes(10),
                cancellationToken,
                onLiveEvent: onLiveEvent);

            return (result.FinalText, modelIdentifier, result);
        }

        /// <summary>
        /// Continues a durable Harness session from its last persisted MAF session state.
        /// </summary>
        public async Task<(string response, string modelIdentifier, AdminChatHarnessResult harness)> ResumeNativeHarnessResponseAsync(
            int trajectoryId,
            int userId,
            string instruction,
            int aiModelId = 0,
            int maxIterations = 32,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default,
            Action<AdminChatLiveEvent> onLiveEvent = null)
        {
            var trajectory = await _trajectoryService.GetAsync(trajectoryId, userId)
                ?? throw new NcfExceptionBase("未找到可恢复的 Harness Trajectory。");
            var (harnessAgent, modelIdentifier) = await BuildNativeHarnessAgentAsync(
                trajectory.SessionId,
                userId,
                aiModelId,
                maxIterations,
                ParseSessionState(trajectory.SessionStateJson),
                cancellationToken);

            var prompt = string.IsNullOrWhiteSpace(instruction)
                ? "请继续执行上一次尚未完成的任务，先检查当前进度，再继续执行。"
                : instruction.Trim();
            await _trajectoryService.AppendAsync(
                trajectory,
                "resume",
                "user",
                "instruction",
                prompt,
                JsonSerializer.Serialize(new { trajectoryId }));

            var result = await ExecuteNativeHarnessAsync(
                trajectory,
                harnessAgent,
                prompt,
                timeout ?? TimeSpan.FromMinutes(10),
                cancellationToken,
                onLiveEvent: onLiveEvent);
            return (result.FinalText, modelIdentifier, result);
        }

        /// <summary>
        /// Creates a new Harness branch from a stored Trajectory point.
        /// </summary>
        public async Task<(string response, string modelIdentifier, AdminChatHarnessResult harness)> ForkNativeHarnessResponseAsync(
            int trajectoryId,
            int userId,
            int branchSessionId,
            int forkFromSequence,
            string instruction,
            int aiModelId = 0,
            int maxIterations = 32,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default,
            Action<AdminChatLiveEvent> onLiveEvent = null)
        {
            var parent = await _trajectoryService.GetAsync(trajectoryId, userId)
                ?? throw new NcfExceptionBase("未找到要分叉的 Harness Trajectory。");
            var events = await _trajectoryService.GetEventsAsync(
                trajectoryId,
                userId,
                forkFromSequence > 0 ? forkFromSequence + 1 : null,
                pageSize: 200);
            var forkContext = BuildForkContext(events);
            var title = string.IsNullOrWhiteSpace(instruction)
                ? $"分叉：{parent.Title}"
                : instruction.Trim();
            var branch = await _trajectoryService.CreateAsync(
                branchSessionId,
                userId,
                title,
                AdminChatMode.Harness,
                parent.ModelIdentifier,
                parent.Id,
                forkFromSequence > 0 ? forkFromSequence : null);
            var (harnessAgent, modelIdentifier) = await BuildNativeHarnessAgentAsync(
                branchSessionId,
                userId,
                aiModelId,
                maxIterations,
                serializedSession: null,
                cancellationToken);

            var prompt = $"这是从 Trajectory #{parent.Id} 的事件 #{forkFromSequence} 分叉出来的新分支。\n"
                + "以下是分叉点之前的执行轨迹摘要，请在此基础上独立完成新任务：\n"
                + forkContext
                + "\n\n新任务："
                + (string.IsNullOrWhiteSpace(instruction) ? "继续完成原任务。" : instruction.Trim());
            await _trajectoryService.AppendAsync(
                branch,
                "fork",
                "user",
                "instruction",
                instruction,
                JsonSerializer.Serialize(new
                {
                    parentTrajectoryId = parent.Id,
                    forkFromSequence,
                    context = forkContext
                }));

            var result = await ExecuteNativeHarnessAsync(
                branch,
                harnessAgent,
                prompt,
                timeout ?? TimeSpan.FromMinutes(10),
                cancellationToken,
                onLiveEvent: onLiveEvent);
            return (result.FinalText, modelIdentifier, result);
        }

        /// <summary>
        /// Resolves one native MAF tool approval and continues the persisted Harness session.
        /// </summary>
        public async Task<(string response, string modelIdentifier, AdminChatHarnessResult harness)> RespondNativeHarnessApprovalAsync(
            int trajectoryId,
            int userId,
            string requestId,
            string toolCallId,
            string toolName,
            string argumentsJson,
            bool approved,
            string reason = null,
            int aiModelId = 0,
            int maxIterations = 32,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default,
            Action<AdminChatLiveEvent> onLiveEvent = null)
        {
            var trajectory = await _trajectoryService.GetAsync(trajectoryId, userId)
                ?? throw new NcfExceptionBase("未找到待审批的 Harness Trajectory。");
            var (harnessAgent, modelIdentifier) = await BuildNativeHarnessAgentAsync(
                trajectory.SessionId,
                userId,
                aiModelId,
                maxIterations,
                ParseSessionState(trajectory.SessionStateJson),
                cancellationToken);

            var arguments = string.IsNullOrWhiteSpace(argumentsJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson)
                    ?? new Dictionary<string, object>();
            var toolCall = new MafFunctionCallContent(toolCallId ?? string.Empty, toolName ?? string.Empty, arguments);
            var responseContent = new MafToolApprovalResponseContent(requestId ?? string.Empty, approved, toolCall)
            {
                Reason = reason
            };
            await _trajectoryService.AppendAsync(
                trajectory,
                "approval.response",
                "user",
                toolName,
                reason,
                JsonSerializer.Serialize(new { requestId, toolCallId, toolName, approved, arguments }));

            var result = await ExecuteNativeHarnessAsync(
                trajectory,
                harnessAgent,
                string.Empty,
                timeout ?? TimeSpan.FromMinutes(10),
                cancellationToken,
                [new ChatMessage(ChatRole.User, [responseContent])],
                onLiveEvent: onLiveEvent);
            return (result.FinalText, modelIdentifier, result);
        }

        private async Task<(AgentKernelHarness harnessAgent, string modelIdentifier)> BuildNativeHarnessAgentAsync(
            int sessionId,
            int userId,
            int aiModelId,
            int maxIterations,
            JsonElement? serializedSession,
            CancellationToken cancellationToken)
        {
            var (setting, modelIdentifier) = await ResolveChatSettingAsync(aiModelId);
            var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
            var workflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(sessionId);
            var agentAiHandler = new AgentAiHandler(setting);
            var build = await BuildAdminChatFunctionsAsync(setting, agentAiHandler, sessionId, userId, modules, workflows, true);
            var iWantToRun = agentAiHandler.IWantTo(setting)
                .ConfigChatModel($"AdminChatHarness-{userId}-{sessionId}", new ChatClientAgentOptions())
                .BuildKernel();

            var harnessAgent = await iWantToRun.BuildHarnessAgentAsync(
                new AgentKernelHarnessOptions
                {
                    MaxContextWindowTokens = 32_768,
                    MaxOutputTokens = 4_000,
                    HarnessOptions = new Microsoft.Agents.AI.HarnessAgentOptions
                    {
                        Name = $"AdminChatHarness-{sessionId}",
                        Description = "NeuCharFramework Admin Chat long-running task agent",
                        ChatOptions = BuildHarnessChatOptions(
                            modules,
                            build.Functions),
                        DisableFileAccess = true,
                        DisableFileMemory = true,
                        DisableAgentSkillsProvider = true,
                        DisableWebSearch = true,
                        MaximumIterationsPerRequest = Math.Max(1, maxIterations)
                    }
                },
                serializedSession: serializedSession,
                cancellationToken: cancellationToken);
            return (harnessAgent, modelIdentifier);
        }

        private static ChatOptions BuildHarnessChatOptions(
            List<AdminChatSessionModule> modules,
            IReadOnlyCollection<AIFunction> functions)
        {
            var chatOptions = new ChatOptions
            {
                Instructions = BuildSystemMessage(modules),
                Tools = functions != null && functions.Count > 0
                    ? functions.Cast<AITool>().ToList()
                    : null,
                MaxOutputTokens = 4_000,
                TopP = 0.9f,
                Temperature = 0.4f
            };

            return chatOptions;
        }

        private static JsonElement? ParseSessionState(string sessionStateJson)
        {
            if (string.IsNullOrWhiteSpace(sessionStateJson))
            {
                return null;
            }

            using var document = JsonDocument.Parse(sessionStateJson);
            return document.RootElement.Clone();
        }

        private static string BuildForkContext(IReadOnlyList<AdminChatTrajectoryEvent> events)
        {
            if (events == null || events.Count == 0)
            {
                return "(分叉点之前没有可用事件。)";
            }

            var text = string.Join(
                "\n",
                events.Select(item =>
                    $"#{item.Sequence} [{item.EventType}] {item.Name ?? item.Source ?? "event"}: {item.Content ?? item.PayloadJson ?? string.Empty}"));
            return text.Length > 12_000 ? text.Substring(text.Length - 12_000) : text;
        }

        private async Task<AdminChatHarnessResult> ExecuteNativeHarnessAsync(
            AdminChatTrajectory trajectory,
            AgentKernelHarness harnessAgent,
            string prompt,
            TimeSpan timeout,
            CancellationToken cancellationToken,
            IEnumerable<ChatMessage> messages = null,
            Action<AdminChatLiveEvent> onLiveEvent = null)
        {
            var output = new StringBuilder();
            var phaseText = new StringBuilder();
            var phaseKey = string.Empty;
            var phaseEventType = string.Empty;
            var pendingApprovals = new List<AdminChatApprovalRequestDto>();
            var trajectoryEvents = new List<AdminChatTrajectoryEventDto>();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await foreach (var update in harnessAgent.RunStreaming(
                                   messages ?? [new ChatMessage(ChatRole.User, prompt)],
                                   cancellationToken: timeoutCts.Token)
                               .WithCancellation(timeoutCts.Token)
                               .ConfigureAwait(false))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        if (!string.IsNullOrEmpty(phaseEventType)
                            && !string.Equals(phaseEventType, "assistant.text", StringComparison.Ordinal))
                        {
                            var completedReasoningEvent = await FlushAssistantPhaseAsync(
                                trajectory,
                                phaseKey,
                                phaseText,
                                trajectoryEvents,
                                phaseEventType);
                            if (completedReasoningEvent != null)
                            {
                                onLiveEvent?.Invoke(new AdminChatLiveEvent
                                {
                                    TrajectoryId = trajectory.Id,
                                    Kind = "trajectory-event",
                                    Text = completedReasoningEvent.Content,
                                    TrajectoryEvent = completedReasoningEvent,
                                    PendingApprovals = pendingApprovals.ToList()
                                });
                            }

                            phaseKey = string.Empty;
                            phaseEventType = string.Empty;
                        }

                        output.Append(update.Text);
                        if (string.IsNullOrEmpty(phaseKey))
                        {
                            phaseKey = $"assistant-phase-{Guid.NewGuid():N}";
                        }

                        phaseEventType = "assistant.text";
                        phaseText.Append(update.Text);
                        onLiveEvent?.Invoke(new AdminChatLiveEvent
                        {
                            TrajectoryId = trajectory.Id,
                            Kind = "assistant-phase",
                            Text = phaseText.ToString(),
                            TrajectoryEvent = CreateLiveAssistantPhase(
                                trajectory,
                                phaseKey,
                                phaseText.ToString(),
                                phaseEventType),
                            PendingApprovals = pendingApprovals.ToList()
                        });
                    }

                    foreach (var content in update.Contents ?? Array.Empty<AIContent>())
                    {
                        if (content is MafTextContent textContent)
                        {
                            // Providers may expose the same streamed text through both update.Text
                            // and TextContent. Do not create a second trajectory item for the duplicate.
                            if (string.IsNullOrEmpty(update.Text)
                                && !string.IsNullOrEmpty(textContent.Text))
                            {
                                if (!string.IsNullOrEmpty(phaseEventType)
                                    && !string.Equals(phaseEventType, "assistant.text", StringComparison.Ordinal))
                                {
                                    var completedReasoningEvent = await FlushAssistantPhaseAsync(
                                        trajectory,
                                        phaseKey,
                                        phaseText,
                                        trajectoryEvents,
                                        phaseEventType);
                                    if (completedReasoningEvent != null)
                                    {
                                        onLiveEvent?.Invoke(new AdminChatLiveEvent
                                        {
                                            TrajectoryId = trajectory.Id,
                                            Kind = "trajectory-event",
                                            Text = completedReasoningEvent.Content,
                                            TrajectoryEvent = completedReasoningEvent,
                                            PendingApprovals = pendingApprovals.ToList()
                                        });
                                    }

                                    phaseKey = string.Empty;
                                    phaseEventType = string.Empty;
                                }

                                output.Append(textContent.Text);
                                if (string.IsNullOrEmpty(phaseKey))
                                {
                                    phaseKey = $"assistant-phase-{Guid.NewGuid():N}";
                                }

                                phaseEventType = "assistant.text";
                                phaseText.Append(textContent.Text);
                                onLiveEvent?.Invoke(new AdminChatLiveEvent
                                {
                                    TrajectoryId = trajectory.Id,
                                    Kind = "assistant-phase",
                                    Text = phaseText.ToString(),
                                    TrajectoryEvent = CreateLiveAssistantPhase(
                                        trajectory,
                                        phaseKey,
                                        phaseText.ToString(),
                                        phaseEventType),
                                    PendingApprovals = pendingApprovals.ToList()
                                });
                            }

                            continue;
                        }

                        if (content is MafTextReasoningContent reasoningContent
                            && !string.IsNullOrEmpty(reasoningContent.Text))
                        {
                            if (!string.IsNullOrEmpty(phaseEventType)
                                && !string.Equals(phaseEventType, "TextReasoningContent", StringComparison.Ordinal))
                            {
                                var completedAssistantEvent = await FlushAssistantPhaseAsync(
                                    trajectory,
                                    phaseKey,
                                    phaseText,
                                    trajectoryEvents,
                                    phaseEventType);
                                if (completedAssistantEvent != null)
                                {
                                    onLiveEvent?.Invoke(new AdminChatLiveEvent
                                    {
                                        TrajectoryId = trajectory.Id,
                                        Kind = "trajectory-event",
                                        Text = completedAssistantEvent.Content,
                                        TrajectoryEvent = completedAssistantEvent,
                                        PendingApprovals = pendingApprovals.ToList()
                                    });
                                }

                                phaseKey = string.Empty;
                                phaseEventType = string.Empty;
                            }

                            if (string.IsNullOrEmpty(phaseKey))
                            {
                                phaseKey = $"assistant-reasoning-{Guid.NewGuid():N}";
                            }

                            phaseEventType = "TextReasoningContent";
                            phaseText.Append(reasoningContent.Text);
                            onLiveEvent?.Invoke(new AdminChatLiveEvent
                            {
                                TrajectoryId = trajectory.Id,
                                Kind = "assistant-phase",
                                Text = phaseText.ToString(),
                                TrajectoryEvent = CreateLiveAssistantPhase(
                                    trajectory,
                                    phaseKey,
                                    phaseText.ToString(),
                                    phaseEventType),
                                PendingApprovals = pendingApprovals.ToList()
                            });
                            continue;
                        }

                        var completedTextEvent = await FlushAssistantPhaseAsync(
                            trajectory,
                            phaseKey,
                            phaseText,
                            trajectoryEvents,
                            phaseEventType);
                        if (completedTextEvent != null)
                        {
                            onLiveEvent?.Invoke(new AdminChatLiveEvent
                            {
                                TrajectoryId = trajectory.Id,
                                Kind = "trajectory-event",
                                Text = completedTextEvent.Content,
                                TrajectoryEvent = completedTextEvent,
                                PendingApprovals = pendingApprovals.ToList()
                            });
                        }
                        phaseKey = string.Empty;
                        phaseEventType = string.Empty;

                        var eventInfo = await AppendTrajectoryContentAsync(
                            trajectory,
                            content,
                            null,
                            pendingApprovals);
                        if (eventInfo != null)
                        {
                            trajectoryEvents.Add(eventInfo);
                            onLiveEvent?.Invoke(new AdminChatLiveEvent
                            {
                                TrajectoryId = trajectory.Id,
                                Kind = "trajectory-event",
                                TrajectoryEvent = eventInfo,
                                PendingApprovals = pendingApprovals.ToList()
                            });
                        }
                    }
                }

                var finalTextEvent = await FlushAssistantPhaseAsync(
                    trajectory,
                    phaseKey,
                    phaseText,
                    trajectoryEvents,
                    phaseEventType);
                if (finalTextEvent != null)
                {
                    onLiveEvent?.Invoke(new AdminChatLiveEvent
                    {
                        TrajectoryId = trajectory.Id,
                        Kind = "trajectory-event",
                        Text = finalTextEvent.Content,
                        TrajectoryEvent = finalTextEvent,
                        PendingApprovals = pendingApprovals.ToList()
                    });
                }

                var sessionState = await harnessAgent.SerializeSessionAsync(cancellationToken: timeoutCts.Token);
                await _trajectoryService.UpdateSessionStateAsync(trajectory, sessionState.GetRawText());
                if (pendingApprovals.Count > 0)
                {
                    await _trajectoryService.MarkWaitingForApprovalAsync(trajectory);
                }
                else
                {
                    await _trajectoryService.MarkCompletedAsync(trajectory);
                }
                onLiveEvent?.Invoke(new AdminChatLiveEvent
                {
                    TrajectoryId = trajectory.Id,
                    Kind = "trajectory-complete",
                    PendingApprovals = pendingApprovals.ToList()
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                trajectory.Cancel();
                await _trajectoryService.SaveObjectAsync(trajectory);
                throw;
            }
            catch (OperationCanceledException)
            {
                trajectory.Cancel();
                await _trajectoryService.SaveObjectAsync(trajectory);
            }
            catch (Exception ex)
            {
                await _trajectoryService.MarkFailedAsync(trajectory, ex);
                throw;
            }

            return new AdminChatHarnessResult
            {
                FinalText = output.ToString().Trim(),
                Completed = trajectory.Status == AdminChatTrajectoryStatus.Completed,
                Status = trajectory.Status,
                TrajectoryId = trajectory.Id,
                TrajectorySequence = trajectory.LastSequence,
                TrajectoryEvents = trajectoryEvents,
                PendingApprovals = pendingApprovals
            };
        }

        private async Task<AdminChatTrajectoryEventDto> FlushAssistantPhaseAsync(
            AdminChatTrajectory trajectory,
            string phaseKey,
            StringBuilder phaseText,
            List<AdminChatTrajectoryEventDto> trajectoryEvents,
            string phaseEventType = "assistant.text")
        {
            if (phaseText == null || phaseText.Length == 0)
            {
                return null;
            }

            var textEvent = await _trajectoryService.AppendAsync(
                trajectory,
                string.IsNullOrEmpty(phaseEventType) ? "assistant.text" : phaseEventType,
                "assistant",
                string.Equals(phaseEventType, "TextReasoningContent", StringComparison.Ordinal)
                    ? "TextReasoningContent"
                    : null,
                phaseText.ToString(),
                null,
                phaseKey);
            var textEventDto = AdminChatTrajectoryEventDto.CreateFromEntity(textEvent);
            trajectoryEvents.Add(textEventDto);
            phaseText.Clear();
            return textEventDto;
        }

        private static AdminChatTrajectoryEventDto CreateLiveAssistantPhase(
            AdminChatTrajectory trajectory,
            string phaseKey,
            string content,
            string eventType = "assistant.text")
        {
            return new AdminChatTrajectoryEventDto
            {
                Sequence = trajectory.LastSequence + 1,
                EventType = string.IsNullOrEmpty(eventType) ? "assistant.text" : eventType,
                Source = "assistant",
                Name = string.Equals(eventType, "TextReasoningContent", StringComparison.Ordinal)
                    ? "TextReasoningContent"
                    : null,
                Content = content,
                CorrelationId = phaseKey,
                IsReplayable = true
            };
        }

        private async Task<AdminChatTrajectoryEventDto> AppendTrajectoryContentAsync(
            AdminChatTrajectory trajectory,
            AIContent content,
            string updateText,
            List<AdminChatApprovalRequestDto> pendingApprovals)
        {
            if (content == null)
            {
                return null;
            }

            var eventType = content.GetType().Name;
            var source = "agent";
            var name = content.GetType().Name;
            var contentText = updateText;
            var payload = SerializeContent(content);

            if (content is MafFunctionCallContent functionCall)
            {
                eventType = "tool.call";
                source = "tool";
                name = functionCall.Name;
                payload = SerializeContent(new { functionCall.CallId, functionCall.Name, functionCall.Arguments });
            }
            else if (content is MafFunctionResultContent functionResult)
            {
                eventType = "tool.result";
                source = "tool";
                payload = SerializeContent(new { functionResult.CallId, functionResult.Result });
            }
            else if (content is MafToolApprovalRequestContent approvalRequest)
            {
                eventType = "approval.request";
                source = "approval";
                var approvalFunctionCall = approvalRequest.ToolCall as MafFunctionCallContent;
                name = approvalFunctionCall?.Name ?? approvalRequest.ToolCall.GetType().Name;
                pendingApprovals.Add(new AdminChatApprovalRequestDto
                {
                    RequestId = approvalRequest.RequestId,
                    ToolCallId = approvalRequest.ToolCall.CallId,
                    ToolName = name,
                    ArgumentsJson = approvalFunctionCall == null
                        ? "{}"
                        : SerializeContent(approvalFunctionCall.Arguments)
                });
            }
            else if (content is MafToolApprovalResponseContent approvalResponse)
            {
                eventType = "approval.response";
                source = "approval";
                name = (approvalResponse.ToolCall as MafFunctionCallContent)?.Name
                    ?? approvalResponse.ToolCall.GetType().Name;
            }
            else if (content is MafTextReasoningContent reasoningContent)
            {
                eventType = "TextReasoningContent";
                source = "assistant";
                name = "TextReasoningContent";
                contentText = reasoningContent.Text;
            }

            var item = await _trajectoryService.AppendAsync(
                trajectory,
                eventType,
                source,
                name,
                contentText,
                payload);
            return AdminChatTrajectoryEventDto.CreateFromEntity(item);
        }

        private static string SerializeContent(object content)
        {
            try
            {
                return JsonSerializer.Serialize(content);
            }
            catch
            {
                return content?.ToString();
            }
        }

        private async Task<AdminChatFunctionBuild> BuildAdminChatFunctionsAsync(
            SenparcAiSetting setting,
            AgentAiHandler agentAiHandler,
            int sessionId,
            int userId,
            List<AdminChatSessionModule> modules,
            List<AdminChatSessionWorkflow> workflows,
            bool functionInvocationEnabled)
        {
            var modulePlugin = new ModuleAssistantPlugin(modules);
            var aiFunctions = functionInvocationEnabled
                ? agentAiHandler.GetAITools(modulePlugin)
                : new List<AIFunction>();

            var importedPluginNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (functionInvocationEnabled)
            {
                importedPluginNames.Add("ModuleAssistant");
            }

            // 自动加载会话关联模块中的 FunctionRender（[#sym:FunctionRender]）插件对象
            var moduleUids = modules.Where(z => !z.XncfModuleUid.IsNullOrEmpty()).Select(z => z.XncfModuleUid).ToList();
            var functionRenderBags = functionInvocationEnabled
                ? moduleUids
                    .SelectMany(uid => Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.GetByModuleUid(uid))
                    // FunctionRender is also used by the normal Admin UI. AI exposure is an
                    // explicit second permission boundary so host-mutating legacy functions are
                    // never imported merely because their module is attached to a chat session.
                    .Where(z => z.MethodInfo != null
                                && z.MethodInfo.DeclaringType != null
                                && z.FunctionRenderAttribute?.AllowAiInvocation != false)
                    .ToList()
                : new List<FunctionRenderBag>();

            var functionPluginGroups = functionRenderBags
                .GroupBy(z => z.MethodInfo.DeclaringType)
                .ToList();

            var importedFunctionCount = 0;
            var importedWorkflowCount = 0;
            var importedFunctionSignatures = new List<string>();
            var loadedFunctionDebugLines = new List<string>();

            foreach (var pluginGroup in functionPluginGroups)
            {
                try
                {
                    var pluginType = pluginGroup.Key;
                    var plugin = _serviceProvider.GetService(pluginType) ?? Activator.CreateInstance(pluginType);
                    if (plugin == null)
                    {
                        _logger.LogWarning("导入 FunctionRender 插件失败：{PluginType}，无法创建实例", pluginType.FullName);
                        continue;
                    }

                    var pluginName = BuildFunctionPluginName(pluginType);
                    var kernelFunctions = new List<KernelFunction>();

                    foreach (var functionBag in pluginGroup.GroupBy(z => z.Key).Select(z => z.First()))
                    {
                        try
                        {
                            var options = new KernelFunctionFromMethodOptions
                            {
                                FunctionName = functionBag.MethodInfo.Name,
                                Description = functionBag.FunctionRenderAttribute?.Description
                            };

                            var kernelFunction = KernelFunctionFactory.CreateFromMethod(functionBag.MethodInfo, plugin, options);
                            kernelFunctions.Add(kernelFunction);

                            IReadOnlyList<FunctionParameterInfo> parameterInfos;
                            try
                            {
                                parameterInfos = await FunctionHelper.GetFunctionParameterInfoAsync(
                                    _serviceProvider,
                                    functionBag,
                                    true).ConfigureAwait(false);
                            }
                            catch (Exception metadataException)
                            {
                                _logger.LogWarning(
                                    metadataException,
                                    "读取 FunctionRender 参数选项失败，将使用基础 schema：Plugin={PluginType}, Method={MethodName}",
                                    pluginType.FullName,
                                    functionBag.MethodInfo.Name);
                                parameterInfos = Array.Empty<FunctionParameterInfo>();
                            }

                            aiFunctions.Add(AdminChatFunctionToolFactory.Create(
                                method: functionBag.MethodInfo,
                                target: plugin,
                                name: BuildFunctionToolName(pluginName, options.FunctionName),
                                description: options.Description,
                                parameterInfos: parameterInfos));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "导入 FunctionRender 方法失败：Plugin={PluginType}, Method={MethodName}",
                                pluginType.FullName,
                                functionBag.MethodInfo?.Name);
                        }
                    }

                    if (kernelFunctions.Count == 0)
                    {
                        _logger.LogWarning("导入 FunctionRender 插件失败：{PluginType}，未生成任何可用 KernelFunction", pluginType.FullName);
                        continue;
                    }

                    var addedPlugin = KernelPluginFactory.CreateFromFunctions(pluginName, kernelFunctions);

                    importedFunctionSignatures.AddRange(addedPlugin.Select(kernelFunction => $"{kernelFunction.Metadata.PluginName}.{kernelFunction.Metadata.Name}({kernelFunction.Metadata.Description ?? "N/A"})"));
                    loadedFunctionDebugLines.AddRange(BuildKernelPluginDebugLines(addedPlugin));
                    importedPluginNames.Add(pluginName);
                    importedFunctionCount += addedPlugin.Count();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入 FunctionRender 插件失败：{PluginType}", pluginGroup.Key?.FullName);
                }
            }

            var workflowProvider = functionInvocationEnabled
                ? _serviceProvider.GetService<IWorkflowFunctionCallingProvider>()
                : null;
            if (workflowProvider != null && workflows.Count > 0)
            {
                try
                {
                    var availableWorkflows = await workflowProvider
                        .GetAvailableAsync(userId)
                        .ConfigureAwait(false);
                    var availableById = availableWorkflows.ToDictionary(workflow => workflow.Id);

                    foreach (var sessionWorkflow in workflows)
                    {
                        if (!availableById.TryGetValue(sessionWorkflow.WorkflowId, out var workflow))
                        {
                            _logger.LogInformation(
                                "跳过不可用的 AdminChat Workflow：SessionId={SessionId}, WorkflowId={WorkflowId}",
                                sessionId,
                                sessionWorkflow.WorkflowId);
                            continue;
                        }

                        var tool = AdminChatWorkflowToolFactory.Create(workflowProvider, userId, workflow);
                        aiFunctions.Add(tool);
                        importedWorkflowCount++;
                        importedFunctionSignatures.Add(
                            $"{tool.Name}({string.Join(", ", workflow.Parameters.Select(parameter => parameter.Name))})");
                        loadedFunctionDebugLines.Add(
                            $"- Workflow: {tool.Name} ({workflow.Name})");
                        loadedFunctionDebugLines.Add(
                            $"  Description: {tool.Description}");
                        loadedFunctionDebugLines.Add(
                            $"  Schema: {tool.JsonSchema.GetRawText()}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入 AdminChat Workflow Function Calling 工具失败：SessionId={SessionId}", sessionId);
                }
            }
            return new AdminChatFunctionBuild
            {
                Functions = aiFunctions,
                PluginNames = importedPluginNames,
                ImportedFunctionCount = importedFunctionCount,
                ImportedWorkflowCount = importedWorkflowCount,
                Signatures = importedFunctionSignatures,
                DebugLines = loadedFunctionDebugLines
            };
        }

        private sealed class AdminChatFunctionBuild
        {
            public List<AIFunction> Functions { get; set; }
            public HashSet<string> PluginNames { get; set; }
            public int ImportedFunctionCount { get; set; }
            public int ImportedWorkflowCount { get; set; }
            public List<string> Signatures { get; set; }
            public List<string> DebugLines { get; set; }
        }

        private static async Task<string> ExecuteRunnerWithSessionRetryAsync(
            IWantToRun runner,
            string prompt,
            Action<AgentResponseUpdate> onUpdate,
            CancellationToken cancellationToken)
        {
            var session = runner?.Kernel?.AgentSession;

            async Task<string> RunStreamingAsync(AgentSession agentSession)
            {
                var output = new StringBuilder();
                await foreach (var update in runner.RunChatStreamingAsync(
                                   prompt,
                                   agentSession,
                                   options: null,
                                   cancellationToken: cancellationToken)
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
                {
                    if (!string.IsNullOrEmpty(update?.Text))
                    {
                        output.Append(update.Text);
                    }

                    onUpdate?.Invoke(update);
                }

                return output.ToString();
            }

            try
            {
                return await RunStreamingAsync(session);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch when (session != null)
            {
                return await RunStreamingAsync(null);
            }
        }

        private async Task<(SenparcAiSetting setting, string modelIdentifier)> ResolveChatSettingAsync(int aiModelId)
        {
            var defaultSetting = Senparc.AI.Config.SenparcAiSetting as SenparcAiSetting;
            if (defaultSetting == null)
            {
                throw new NcfExceptionBase("未读取到 SenparcAiSetting，请检查 appsettings.json 配置。");
            }

            if (defaultSetting.AiPlatform == AiPlatform.UnSet)
            {
                throw new NcfExceptionBase("SenparcAiSetting.AiPlatform 仍为 UnSet，请先在 appsettings.json 中设置可用平台。");
            }

            if (aiModelId <= 0)
            {
                return (defaultSetting, ResolveModelIdentifier(defaultSetting));
            }

            if (!await IsAiKernelAvailableAsync())
            {
                throw new NcfExceptionBase("当前系统未安装或未启用 AIKernel 模块，无法切换到指定 AI 模型。");
            }

            var aiModelService = _serviceProvider.GetService(typeof(AIModelService)) as AIModelService;
            if (aiModelService == null)
            {
                throw new NcfExceptionBase("未能解析 AIModelService，无法加载指定 AI 模型。");
            }

            var aiModel = await aiModelService.GetObjectAsync(z => z.Id == aiModelId);
            if (aiModel == null)
            {
                throw new NcfExceptionBase($"当前选择的 AI 模型不存在：{aiModelId}");
            }

            if (aiModel.ConfigModelType != Senparc.Xncf.AIKernel.Domain.Models.ConfigModelType.Chat)
            {
                throw new NcfExceptionBase($"当前选择的 AI 模型不是 Chat 类型：{aiModelId}");
            }

            var aiModelDto = aiModelService.Mapper.Map<AIModelDto>(aiModel);
            var selectedSetting = aiModelService.BuildSenparcAiSetting(aiModelDto);
            var selectedModelIdentifier = !string.IsNullOrWhiteSpace(aiModelDto.Alias)
                ? $"{aiModelDto.Alias} [{ResolveModelIdentifier(selectedSetting)}]"
                : ResolveModelIdentifier(selectedSetting);

            return (selectedSetting, selectedModelIdentifier);
        }

        private async Task<bool> IsAiKernelAvailableAsync()
        {
            var aiKernelRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z =>
                string.Equals(z.Name, "Senparc.Xncf.AIKernel", StringComparison.OrdinalIgnoreCase));

            if (aiKernelRegister == null)
            {
                return false;
            }

            var registerManager = new XncfRegisterManager(_serviceProvider);
            return await registerManager.CheckXncfAvailable(aiKernelRegister);
        }

        private static string BuildFunctionPluginName(Type pluginType)
        {
            var fullName = pluginType?.FullName ?? pluginType?.Name ?? "FunctionPlugin";
            var normalized = fullName.Replace('.', '_').Replace('+', '_');

            // OpenAI function name has max length 64, reserve space for method suffix and separators.
            // Use deterministic short hash suffix to keep uniqueness across modules.
            const int maxPluginNameLength = 36;
            const int hashLength = 8;
            var hash = ComputeShortHash(normalized, hashLength);

            var prefixMaxLength = maxPluginNameLength - ("Xncf_".Length + 1 + hashLength);
            var prefix = normalized.Length > prefixMaxLength ? normalized.Substring(0, prefixMaxLength) : normalized;

            return $"Xncf_{prefix}_{hash}";
        }

        private static string BuildFunctionToolName(string pluginName, string methodName)
        {
            var normalizedPluginName = string.IsNullOrWhiteSpace(pluginName) ? "FunctionPlugin" : pluginName;
            var normalizedMethodName = string.IsNullOrWhiteSpace(methodName) ? "Invoke" : methodName;
            var candidate = $"{normalizedPluginName}_{normalizedMethodName}";
            const int maxFunctionNameLength = 64;
            if (candidate.Length <= maxFunctionNameLength)
            {
                return candidate;
            }

            const int hashLength = 8;
            var hash = ComputeShortHash(candidate, hashLength);
            var prefixLength = maxFunctionNameLength - hashLength - 1;
            return $"{candidate.Substring(0, prefixLength)}_{hash}";
        }

        private static string ComputeShortHash(string input, int length)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            var hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower(CultureInfo.InvariantCulture);
            return hex.Length > length ? hex.Substring(0, length) : hex;
        }

        private static void WriteLoadedFunctionsToConsole(int sessionId, int userId, List<string> loadedFunctionDebugLines)
        {
            if (loadedFunctionDebugLines == null || loadedFunctionDebugLines.Count == 0)
            {
                return;
            }

            Console.WriteLine($"[AdminChat Functions] SessionId={sessionId}, UserId={userId}, LoadedFunctions={loadedFunctionDebugLines.Count(line => line.StartsWith("- Function:", StringComparison.Ordinal))}");
            foreach (var line in loadedFunctionDebugLines)
            {
                Console.WriteLine(line);
            }
        }

        private static List<string> BuildKernelPluginDebugLines(KernelPlugin kernelPlugin)
        {
            var lines = new List<string>();
            foreach (var kernelFunction in kernelPlugin)
            {
                lines.AddRange(BuildKernelFunctionDebugLines(kernelFunction));
            }

            return lines;
        }

        private static List<string> BuildKernelFunctionDebugLines(KernelFunction kernelFunction)
        {
            var metadata = kernelFunction.Metadata;
            var functionParameters = metadata.Parameters?.ToList() ?? new List<KernelParameterMetadata>();
            var lines = new List<string>
            {
                $"- Function: {metadata.PluginName}.{metadata.Name}",
                $"  Description: {metadata.Description ?? "(none)"}",
                $"  ReturnType: {metadata.ReturnParameter.ParameterType?.FullName ?? "(none)"}",
                $"  ReturnSchema: {FormatSchema(metadata.ReturnParameter.Schema)}"
            };

            if (functionParameters == null || functionParameters.Count == 0)
            {
                lines.Add("  Parameters: (none)");
                return lines;
            }

            lines.Add($"  Parameters: {functionParameters.Count}");
            foreach (var parameter in functionParameters)
            {
                lines.Add($"    - {parameter.Name}: type={parameter.ParameterType?.FullName ?? "(none)"}, required={parameter.IsRequired}, description={FormatInlineValue(parameter.Description)}, default={FormatParameterValue(parameter.DefaultValue)}, schema={FormatSchema(parameter.Schema)}");
            }

            return lines;
        }

        private static string FormatParameterValue(object value)
        {
            if (value == null)
            {
                return "(null)";
            }

            if (value is string stringValue)
            {
                return FormatInlineValue(stringValue);
            }

            if (value is IEnumerable<string> stringValues)
            {
                return $"[{string.Join(", ", stringValues.Select(FormatInlineValue))}]";
            }

            return FormatInlineValue(value.ToString());
        }

        private static string FormatSchema(KernelJsonSchema schema)
        {
            return schema == null ? "(none)" : FormatInlineValue(schema.ToString());
        }

        private static string FormatInlineValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(empty)";
            }

            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string BuildSystemMessage(List<AdminChatSessionModule> modules)
        {
            var sb = new StringBuilder();
            sb.AppendLine("你是 NeuCharFramework 管理后台首页中的 AI 智能助手。");
            sb.AppendLine("请使用简洁、准确、可执行的中文回答用户问题。若信息不足，请明确指出缺失信息。\n");

            if (modules != null && modules.Any())
            {
                sb.AppendLine("当前会话关联模块如下，可优先结合这些模块语境回答。如需深入了解模块详情、数据库结构或功能列表，可使用 ModuleAssistant 函数获取准确信息：");
                foreach (var module in modules)
                {
                    sb.AppendLine($"- **{module.ModuleName}** (UID: {module.XncfModuleUid}, Version: {module.ModuleVersion})");
                    var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == module.XncfModuleUid);
                    if (register != null && !string.IsNullOrWhiteSpace(register.Description))
                        sb.AppendLine($"  描述：{register.Description}");
                }
            }

            return sb.ToString();
        }

        private static string BuildUserPrompt(List<AdminChatMessage> messages, string currentUserMessage)
        {
            var history = (messages ?? new List<AdminChatMessage>())
                .OrderBy(m => m.Sequence)
                .TakeLast(12)
                .Select(m => $"[{GetRoleName(m.RoleType)}] {m.Content}");

            return "以下是最近对话上下文，请在保持语义连贯的前提下回答最后一个用户问题。\n\n"
                 + string.Join("\n", history)
                 + $"\n\n[用户当前问题] {currentUserMessage}";
        }

        private static string GetRoleName(ChatMessageRoleType roleType)
        {
            return roleType switch
            {
                ChatMessageRoleType.User => "用户",
                ChatMessageRoleType.Assistant => "助手",
                ChatMessageRoleType.System => "系统",
                _ => "未知"
            };
        }

        private static string ResolveModelIdentifier(SenparcAiSetting setting)
        {
            var modelName = setting.NeuCharAIKeys?.ModelName?.Chat
                            ?? setting.AzureOpenAIKeys?.ModelName?.Chat
                            ?? setting.AzureOpenAIKeys?.DeploymentName
                            ?? setting.OpenAIKeys?.ModelName?.Chat
                            ?? setting.HuggingFaceKeys?.ModelName?.Chat
                            ?? setting.DeepSeekKeys?.ModelName?.Chat
                            ?? "unknown";

            return $"{setting.AiPlatform}:{modelName}";
        }
    }
}
