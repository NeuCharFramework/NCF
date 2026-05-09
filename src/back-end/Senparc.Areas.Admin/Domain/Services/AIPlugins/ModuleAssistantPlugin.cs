using Microsoft.SemanticKernel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Services.AIPlugins
{
    /// <summary>
    /// ModuleAssistantPlugin：针对会话关联模块的 AI Function Calling 插件。
    /// <para>当用户提问涉及模块信息、数据库结构、已安装功能等时，AI 将自动调用对应函数。</para>
    /// </summary>
    public class ModuleAssistantPlugin
    {
        private readonly List<AdminChatSessionModule> _sessionModules;

        /// <summary>
        /// 初始化模块助手插件。
        /// </summary>
        /// <param name="sessionModules">当前会话关联模块列表。</param>
        public ModuleAssistantPlugin(List<AdminChatSessionModule> sessionModules)
        {
            _sessionModules = sessionModules ?? new List<AdminChatSessionModule>();
        }

        /// <summary>
        /// 列出当前会话关联的所有 XNCF 模块
        /// </summary>
        [KernelFunction, Description("获取当前对话会话已关联的所有 XNCF 模块列表。当用户询问当前会话有哪些模块、关联了哪些模块时调用。")]
        public string GetSessionModuleList()
        {
            if (!_sessionModules.Any())
                return "当前会话没有关联任何 XNCF 模块。";

            var sb = new StringBuilder();
            sb.AppendLine($"当前会话已关联 {_sessionModules.Count} 个模块：");
            foreach (var m in _sessionModules)
            {
                var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == m.XncfModuleUid);
                sb.AppendLine($"\n- **{m.ModuleName}**");
                sb.AppendLine($"  UID: {m.XncfModuleUid}");
                sb.AppendLine($"  版本: {m.ModuleVersion}");
                if (register != null)
                {
                    sb.AppendLine($"  描述: {register.Description}");
                    sb.AppendLine($"  菜单名: {register.MenuName}");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取指定模块的详细信息（含 Functions 列表）
        /// </summary>
        [KernelFunction, Description("获取指定模块的详细信息，包括描述、版本、可用功能（FunctionRender）列表等。支持通过模块名称关键字或 UID 查询。当用户询问某个模块有哪些功能、模块说明时调用。")]
        public string GetModuleDetail(
            [Description("模块名称关键字（支持模糊匹配）或模块 UID")] string moduleUidOrName)
        {
            var module = FindModule(moduleUidOrName);
            if (module == null)
                return $"未找到匹配的模块：\"{moduleUidOrName}\"。请先调用 GetSessionModuleList 确认当前会话中的模块列表。";

            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == module.XncfModuleUid);
            var sb = new StringBuilder();
            sb.AppendLine($"## 模块详情：{module.ModuleName}");
            sb.AppendLine($"- **UID**: {module.XncfModuleUid}");
            sb.AppendLine($"- **版本**: {module.ModuleVersion}");

            if (register != null)
            {
                sb.AppendLine($"- **程序集名称**: {register.Name}");
                sb.AppendLine($"- **菜单名**: {register.MenuName}");
                sb.AppendLine($"- **描述**: {register.Description}");
                sb.AppendLine($"- **图标**: {register.Icon}");
                sb.AppendLine($"- **支持 MCP**: {(register.EnableMcpServer ? "是" : "否")}");

                // 获取 FunctionRender 注册的功能列表
                if (Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(register.GetType(), out var functionGroup) && functionGroup.Any())
                {
                    sb.AppendLine($"\n### 可用功能（FunctionRender，共 {functionGroup.Count} 个）：");
                    foreach (var f in functionGroup.Values)
                    {
                        sb.AppendLine($"- **{f.FunctionRenderAttribute.Name}**：{f.FunctionRenderAttribute.Description}");
                    }
                }
                else
                {
                    sb.AppendLine("\n此模块未注册 FunctionRender 功能。");
                }
            }
            else
            {
                sb.AppendLine("（该模块已关联会话，但运行时 XncfRegisterManager 中未找到其注册，可能已卸载。）");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取指定模块的数据库结构信息
        /// </summary>
        [KernelFunction, Description("获取指定模块的数据库/存储结构信息，包括数据库前缀、DbContext 类型、数据表（DbSet）列表等。当用户询问模块的数据结构、数据库表、存储内容时调用。")]
        public string GetModuleDatabaseInfo(
            [Description("模块名称关键字或模块 UID")] string moduleUidOrName)
        {
            var module = FindModule(moduleUidOrName);
            if (module == null)
                return $"未找到模块：\"{moduleUidOrName}\"。请先调用 GetSessionModuleList 确认可用模块。";

            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == module.XncfModuleUid);
            if (register == null)
                return $"模块 {module.ModuleName} 的运行时注册未找到，无法读取数据库信息。";

            var sb = new StringBuilder();
            sb.AppendLine($"## {module.ModuleName} 数据库信息");

            if (register is IXncfDatabase dbRegister)
            {
                sb.AppendLine($"- **数据库表前缀**: {dbRegister.DatabaseUniquePrefix}");
                var ctxType = dbRegister.TryGetXncfDatabaseDbContextType;
                sb.AppendLine($"- **DbContext 类型**: {ctxType?.FullName ?? "未获取到"}");

                if (ctxType != null)
                {
                    var dbSetProps = ctxType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.PropertyType.IsGenericType &&
                                    p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("DbSet"))
                        .ToList();

                    if (dbSetProps.Any())
                    {
                        sb.AppendLine($"- **数据表（DbSet，共 {dbSetProps.Count} 张）**：");
                        foreach (var prop in dbSetProps)
                        {
                            var entityType = prop.PropertyType.GetGenericArguments().FirstOrDefault();
                            sb.AppendLine($"  * **{prop.Name}** → 实体类型：{entityType?.Name ?? "未知"}");

                            // 列出实体的公开属性（简要字段清单）
                            if (entityType != null)
                            {
                                var fields = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(p => !p.PropertyType.IsGenericType || p.PropertyType.GetGenericTypeDefinition() != typeof(Lazy<>))
                                    .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                                    .Select(p => $"{p.Name}（{p.PropertyType.Name}）")
                                    .Take(12)
                                    .ToList();
                                if (fields.Any())
                                    sb.AppendLine($"    字段：{string.Join(", ", fields)}");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine("  未检测到 DbSet 属性（或无公开数据表）。");
                    }
                }
            }
            else
            {
                sb.AppendLine("此模块没有独立数据库（未实现 IXncfDatabase 接口）。");
                sb.AppendLine("该模块可能使用公共数据库，或不涉及持久化存储。");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 列出系统中所有已注册模块
        /// </summary>
        [KernelFunction, Description("列出当前系统中所有已注册的 XNCF 模块（不局限于当前会话的关联模块）。当用户想了解系统整体有哪些模块、模块概览时调用。")]
        public string ListAllInstalledModules()
        {
            var allModules = XncfRegisterManager.RegisterList
                .Where(z => !z.IgnoreInstall)
                .OrderBy(z => z.MenuName)
                .ToList();

            if (!allModules.Any())
                return "系统中没有已注册的模块。";

            var sb = new StringBuilder();
            sb.AppendLine($"系统共注册 {allModules.Count} 个模块：");
            foreach (var r in allModules)
            {
                var inSession = _sessionModules.Any(m => m.XncfModuleUid == r.Uid);
                sb.AppendLine($"- **{r.MenuName}** ({r.Name}) v{r.Version}{(inSession ? "  ✓[当前会话已关联]" : "")}");
                if (!string.IsNullOrWhiteSpace(r.Description))
                    sb.AppendLine($"  {r.Description}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 根据 UID 或名称关键字从会话模块中查找
        /// </summary>
        private AdminChatSessionModule FindModule(string moduleUidOrName)
        {
            if (string.IsNullOrWhiteSpace(moduleUidOrName)) return null;
            return _sessionModules.FirstOrDefault(m =>
                m.XncfModuleUid.Equals(moduleUidOrName, StringComparison.OrdinalIgnoreCase) ||
                m.ModuleName.Contains(moduleUidOrName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
