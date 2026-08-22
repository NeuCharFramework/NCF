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
