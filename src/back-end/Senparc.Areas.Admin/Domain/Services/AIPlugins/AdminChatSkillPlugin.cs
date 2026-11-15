/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminChatSkillPlugin.cs
    文件功能描述：后台对话 Skill 插件
    
    
    创建标识：Senparc - 20261016
    
    修改标识：Senparc - 20261021
    修改描述：v0.11.1 把已发布 Skill 注入后台对话

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 update AdminChat with PromptRange Skill integration

----------------------------------------------------------------*/

using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Areas.Admin.Domain.Services.AIPlugins;

/// <summary>
/// Provides the current AdminChat with the published Skill catalog through abstractions.
/// </summary>
public sealed class AdminChatSkillPlugin
{
    private readonly ISkillCatalog _catalog;
    private readonly ISkillExecutor _executor;
    private readonly ISkillPackageService _packageService;
    private readonly HashSet<string> _loadedSkillIds = new(System.StringComparer.OrdinalIgnoreCase);

    public AdminChatSkillPlugin(
        ISkillCatalog catalog,
        ISkillExecutor executor,
        ISkillPackageService packageService)
    {
        _catalog = catalog;
        _executor = executor;
        _packageService = packageService;
    }

    [KernelFunction]
    [Description("List published Skills available to AdminChat.")]
    public async Task<string> ListSkillsAsync()
    {
        var skills = await _catalog.ListAsync(new SkillQuery(
            PublishedOnly: true,
            CallerModule: "AdminChat"));
        return JsonSerializer.Serialize(skills.Select(skill => new
        {
            skill.SkillId,
            skill.Name,
            skill.Description,
            skill.CurrentVersion,
            Versions = skill.Versions.Select(version => version.PromptCode)
        }));
    }

    [KernelFunction]
    [Description("Load one published Skill into the current AdminChat turn so it can be used. Always call this before ExecuteSkill.")]
    public async Task<string> LoadSkillAsync(
        [Description("Stable SkillId or SkillCode")] string skillId)
    {
        var skill = await _catalog.GetAsync(
            skillId,
            context: new SkillExecutionContext("AdminChat", Mode: SkillExecutionMode.Production));
        _loadedSkillIds.Add(skill.SkillId);
        return JsonSerializer.Serialize(new
        {
            skill.SkillId,
            skill.Name,
            skill.CurrentVersion,
            Loaded = true
        });
    }

    [KernelFunction]
    [Description("Execute a published Skill by SkillId with user input.")]
    public async Task<string> ExecuteSkillAsync(
        [Description("Stable SkillId or SkillCode")] string skillId,
        [Description("User input passed to the Skill")] string input,
        [Description("Optional concrete PromptCode version")] string version = null)
    {
        if (!_loadedSkillIds.Contains(skillId))
        {
            return "该 Skill 尚未加载。请先调用 LoadSkillAsync，并确认使用的 SkillId。";
        }

        var result = await _executor.ExecuteAsync(new SkillExecutionRequest(
            skillId,
            version,
            input,
            new SkillExecutionContext("AdminChat")));
        return result.Output;
    }

    [KernelFunction]
    [Description("Install a Skill package from a HTTPS URL after the administrator explicitly requests installation. It installs as Draft and never publishes or executes it automatically.")]
    public async Task<string> InstallSkillFromUrlAsync(
        [Description("HTTPS URL of the Skill ZIP package")] string packageUrl)
    {
        var result = await _packageService.InstallFromUrlAsync(packageUrl);
        return JsonSerializer.Serialize(new
        {
            result.Skill.SkillId,
            result.Skill.Name,
            result.Skill.CurrentVersion,
            result.RequiresApproval,
            result.Message
        });
    }
}
