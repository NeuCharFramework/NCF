/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatSessionWorkflowService.cs
    文件功能描述：AdminChatSessionWorkflowService.cs 相关实现


    创建标识：Senparc - 20260821

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class AdminChatSessionWorkflowService : BaseClientService<AdminChatSessionWorkflow>
{
    public AdminChatSessionWorkflowService(
        IAdminChatSessionWorkflowRepository repository,
        IServiceProvider serviceProvider)
        : base(repository, serviceProvider)
    {
    }

    public async Task<List<AdminChatSessionWorkflow>> GetSessionWorkflowsAsync(int sessionId)
    {
        var workflows = await GetFullListAsync(
            workflow => workflow.SessionId == sessionId,
            "AddedTime ASC");
        return workflows.ToList();
    }

    public async Task AddWorkflowsToSessionAsync(
        int sessionId,
        IEnumerable<(int id, string name, string description)> workflows)
    {
        foreach (var workflow in workflows ?? Enumerable.Empty<(int, string, string)>())
        {
            if (workflow.id <= 0)
            {
                continue;
            }

            var existing = await GetObjectAsync(item =>
                item.SessionId == sessionId &&
                item.WorkflowId == workflow.id);
            if (existing != null)
            {
                continue;
            }

            await SaveObjectAsync(new AdminChatSessionWorkflow(
                sessionId,
                workflow.id,
                workflow.name,
                workflow.description));
        }
    }

    public async Task SetSessionWorkflowsAsync(
        int sessionId,
        IEnumerable<(int id, string name, string description)> workflows)
    {
        var desired = (workflows ?? Enumerable.Empty<(int, string, string)>())
            .Where(workflow => workflow.id > 0)
            .GroupBy(workflow => workflow.id)
            .Select(group => group.First())
            .ToList();
        var desiredIds = desired.Select(workflow => workflow.id).ToHashSet();
        var existing = await GetSessionWorkflowsAsync(sessionId);

        await AddWorkflowsToSessionAsync(sessionId, desired);
        foreach (var obsolete in existing.Where(workflow => !desiredIds.Contains(workflow.WorkflowId)))
        {
            await DeleteObjectAsync(obsolete);
        }
    }
}
