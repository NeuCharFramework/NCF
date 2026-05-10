using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
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

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// AdminChatAiService：管理后台聊天 AI 调用服务。
    /// 默认使用 appsettings 中的 SenparcAiSetting，也支持按请求切换到 AIKernel 中配置的 Chat 模型。
    /// </summary>
    public class AdminChatAiService
    {
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;
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
            IServiceProvider serviceProvider,
            ILogger<AdminChatAiService> logger)
        {
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
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
        public async Task<(string response, string modelIdentifier)> GenerateResponseAsync(int sessionId, int userId, string userMessage, int aiModelId = 0)
        {
            var showLoadedFunctionsInConsole = true;//是否输出 function 的 schema 信息到控制台，便于调试和验证 Function Calling 功能是否正确加载了函数

            var (setting, modelIdentifier) = await ResolveChatSettingAsync(aiModelId);

            var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
            var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);

            var semanticAiHandler = new SemanticAiHandler(setting);
            var promptParameter = new PromptConfigParameter
            {
                MaxTokens = 2000,
                Temperature = 0.6f,
                TopP = 0.9f
            };

            var modulePlugin = new ModuleAssistantPlugin(modules);

            var iWantToRun = semanticAiHandler.ChatConfig(
                promptParameter,
                userId: $"AdminChat-{userId}-{sessionId}",
                maxHistoryStore: 20,
                chatSystemMessage: BuildSystemMessage(modules),
                senparcAiSetting: setting);

            // 注册模块信息 Function Calling 插件
            iWantToRun.ImportPluginFromObject(modulePlugin, "ModuleAssistant");
            var importedPluginNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ModuleAssistant" };

            // 自动加载会话关联模块中的 FunctionRender（[#sym:FunctionRender]）插件对象
            var moduleUids = modules.Where(z => !z.XncfModuleUid.IsNullOrEmpty()).Select(z => z.XncfModuleUid).ToList();
            var functionRenderBags = moduleUids
                .SelectMany(uid => Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.GetByModuleUid(uid))
                .Where(z => z.MethodInfo != null && z.MethodInfo.DeclaringType != null)
                .ToList();

            var functionPluginGroups = functionRenderBags
                .GroupBy(z => z.MethodInfo.DeclaringType)
                .ToList();

            var importedFunctionCount = 0;
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

                    var addedPlugin = iWantToRun.Kernel.Plugins.AddFromFunctions(pluginName, kernelFunctions);
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

            if (importedFunctionCount == 0)
            {
                _logger.LogWarning(
                    "AdminChat 未注入任何 FunctionRender 函数：SessionId={SessionId}, UserId={UserId}, ModuleCount={ModuleCount}, Modules={Modules}",
                    sessionId,
                    userId,
                    moduleUids.Count,
                    string.Join(",", moduleUids));
            }
            else
            {
                if (showLoadedFunctionsInConsole)
                {
                    WriteLoadedFunctionsToConsole(sessionId, userId, loadedFunctionDebugLines);
                }
            }

            _logger.LogInformation(
                "AdminChat FunctionCalling 插件加载完成：SessionId={SessionId}, UserId={UserId}, ModuleCount={ModuleCount}, Modules={Modules}, Plugins={Plugins}, Functions={Functions}, FunctionList={FunctionList}",
                sessionId,
                userId,
                moduleUids.Count,
                string.Join(",", moduleUids),
                string.Join(",", importedPluginNames),
                importedFunctionCount,
                string.Join(" | ", importedFunctionSignatures));

            var prompt = BuildUserPrompt(messages, userMessage);

            // 使用 FunctionChoiceBehavior.Auto() 让 AI 根据需要自动调用 ModuleAssistantPlugin 函数
            var executionSettings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
            var skResult = await iWantToRun.Kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

            var result = skResult?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("AI 返回空内容：SessionId={SessionId}, UserId={UserId}", sessionId, userId);
                result = "抱歉，我暂时没有生成有效回复，请稍后再试。";
            }

            return (result, modelIdentifier);
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