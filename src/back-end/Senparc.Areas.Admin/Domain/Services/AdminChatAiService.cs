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

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.AgentKernel;
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.AI;
using Senparc.AI.AgentKernel.IWantToExtensions;
using Senparc.AI.AgentKernel.Extensions;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

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
            IServiceProvider serviceProvider,
            ILogger<AdminChatAiService> logger)
        {
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
            _sessionWorkflowService = sessionWorkflowService;
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
            AdminChatGenerationOptions generationOptions = null)
        {
            var showLoadedFunctionsInConsole = true;//是否输出 function 的 schema 信息到控制台，便于调试和验证 Function Calling 功能是否正确加载了函数

            var (setting, modelIdentifier) = await ResolveChatSettingAsync(aiModelId);

            var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
            var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
            var workflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(sessionId);

            var agentAiHandler = new AgentAiHandler(setting);


            var functionInvocationEnabled = generationOptions?.AllowFunctionInvocation != false;
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

                            aiFunctions.Add(AdminChatFunctionToolFactory.Create(
                                method: functionBag.MethodInfo,
                                target: plugin,
                                name: BuildFunctionToolName(pluginName, options.FunctionName),
                                description: options.Description));
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
            var streamedOutput = new StringBuilder();
            var hasStreamedChunk = false;
            var skResult = onChunk == null
                ? await iWantToRun.RunChatAsync(prompt)
                : await ExecuteRunnerWithSessionRetryAsync(
                    iWantToRun,
                    prompt,
                    update =>
                    {
                        var updateText = update?.Text;
                        if (string.IsNullOrEmpty(updateText))
                        {
                            return;
                        }

                        streamedOutput.Append(updateText);
                        hasStreamedChunk = true;
                        onChunk(updateText);
                    });

            var result = string.IsNullOrWhiteSpace(skResult?.OutputString)
                ? streamedOutput.ToString().Trim()
                : skResult.OutputString.Trim();
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

        private static async Task<SenparcKernelAiResult<string>> ExecuteRunnerWithSessionRetryAsync(
            IWantToRun runner,
            string prompt,
            Action<AgentResponseUpdate> onUpdate)
        {
            var session = runner?.Kernel?.AgentSession;
            try
            {
                return await runner.RunChatAsync(prompt, session, onUpdate);
            }
            catch when (session != null)
            {
                return await runner.RunChatAsync(prompt, null, onUpdate);
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
