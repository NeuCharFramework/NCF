/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatSessionWorkflowDto.cs
    文件功能描述：AdminChatSessionWorkflowDto.cs 相关实现


    创建标识：Senparc - 20260821

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;

public sealed class AdminChatSessionWorkflowDto : DtoBase<int>
{
    public int SessionId { get; set; }
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; }
    public string WorkflowDescription { get; set; }
    public DateTime AddedTime { get; set; }
    public List<WorkflowFunctionCallingParameterDto> Parameters { get; set; } = new();

    public static AdminChatSessionWorkflowDto CreateFromEntity(AdminChatSessionWorkflow entity) =>
        entity == null
            ? null
            : new AdminChatSessionWorkflowDto
            {
                Id = entity.Id,
                AddTime = entity.AddTime,
                LastUpdateTime = entity.LastUpdateTime,
                TenantId = entity.TenantId,
                Flag = entity.Flag,
                SessionId = entity.SessionId,
                WorkflowId = entity.WorkflowId,
                WorkflowName = entity.WorkflowName,
                WorkflowDescription = entity.WorkflowDescription,
                AddedTime = entity.AddedTime
            };
}

public sealed class WorkflowFunctionCallingParameterDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}
