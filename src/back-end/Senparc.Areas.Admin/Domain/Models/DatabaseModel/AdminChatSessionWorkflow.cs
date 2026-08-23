/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatSessionWorkflow.cs
    文件功能描述：AdminChatSessionWorkflow.cs 相关实现


    创建标识：Senparc - 20260821

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// AdminChat 会话与 Workflow 的独立关联。
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AdminChatSessionWorkflow))]
[Serializable]
public class AdminChatSessionWorkflow : EntityBase<int>
{
    [Required]
    public int SessionId { get; private set; }

    [Required]
    public int WorkflowId { get; private set; }

    [Required, MaxLength(200)]
    public string WorkflowName { get; private set; }

    [MaxLength(400)]
    public string WorkflowDescription { get; private set; }

    [Required]
    public DateTime AddedTime { get; private set; }

    [ForeignKey(nameof(SessionId))]
    public virtual AdminChatSession Session { get; private set; }

    private AdminChatSessionWorkflow() { }

    public AdminChatSessionWorkflow(
        int sessionId,
        int workflowId,
        string workflowName,
        string workflowDescription)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId));
        }
        if (workflowId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workflowId));
        }

        SessionId = sessionId;
        WorkflowId = workflowId;
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? $"Workflow #{workflowId}" : workflowName.Trim();
        WorkflowDescription = workflowDescription?.Trim();
        AddedTime = DateTime.Now;
    }
}
