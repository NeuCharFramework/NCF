/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Aggregate.cshtml.cs
    文件功能描述：集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
public class AggregateModel(
    IServiceProvider serviceProvider,
    NeuCharPivotService pivotService,
    NeuCharPivotFunctionService functionEntityService,
    NeuCharFunctionService functionService,
    NeuCharExecutionLogService logService,
    NeuCharPivotNeuBellProvider neuBellProvider,
    INeuBellPublisher neuBellPublisher,
    IAdminWorkContextProvider adminWorkContextProvider) : BaseAdminPageModel(serviceProvider)
{
    public async Task OnGetAsync()
    {
        if (neuBellProvider.ConsumeAll() > 0)
        {
            await neuBellPublisher.NotifyChangedAsync(
                NeuCharPivotNeuBellProvider.ProviderName,
                HttpContext.RequestAborted).ConfigureAwait(false);
        }
    }

    public async Task<IActionResult> OnGetListAsync()
    {
        var snapshots = await pivotService.GetAllSnapshotsAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(snapshots.Select(snapshot => new
        {
            configuration = new
            {
                snapshot.Configuration.Id,
                snapshot.Configuration.ModuleUid,
                snapshot.Configuration.Name,
                snapshot.Configuration.LayoutSchemaJson,
                snapshot.Configuration.Revision,
                snapshot.Configuration.LastGeneratedAt,
                snapshot.Configuration.LastError
            },
            snapshot.ModuleAvailable,
            snapshot.ModuleState,
            functions = snapshot.Functions.Where(z => z.Visible).Select(function => new
            {
                function.Id,
                function.FunctionKey,
                function.FunctionName,
                function.Description,
                parameterSchemaJson = function.UiSchemaJson,
                function.DefaultParametersJson,
                function.ModuleVersion,
                available = snapshot.FunctionAvailability.TryGetValue(function.Id, out var available) && available,
                loopTask = snapshot.LoopTasks.TryGetValue(function.Id, out var task) ? new
                {
                    task.Enabled,
                    task.IntervalSeconds,
                    task.UseNeuBell,
                    task.LastRunAt,
                    task.LastSucceeded,
                    task.LastError
                } : null
            })
        }));
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] AggregateRunRequest request)
    {
        if (request == null || request.FunctionId <= 0)
        {
            return BadRequest("NeuCharPivot Function 请求无效。");
        }
        var function = await functionEntityService.GetObjectAsync(z => z.Id == request.FunctionId)
            .ConfigureAwait(false);
        if (function == null || !function.Visible)
        {
            return BadRequest("NeuCharPivot Function 不存在或已失效。");
        }

        var correlationId = $"pivot-{Guid.NewGuid():N}";
        var log = new NeuCharExecutionLog(
            "pivot",
            function.Id,
            function.ModuleUid,
            function.FunctionKey,
            function.FunctionName,
            correlationId);
        await logService.SaveObjectAsync(log).ConfigureAwait(false);
        var result = await functionService.ExecuteAsync(
            function.ModuleUid,
            function.FunctionKey,
            request.ParametersJson,
            HttpContext.RequestAborted).ConfigureAwait(false);
        log.Complete(result.Success, result.Data?.ToString(), result.ErrorMessage);
        await logService.SaveObjectAsync(log).ConfigureAwait(false);
        return Ok(result);
    }

    public sealed class AggregateRunRequest
    {
        public int FunctionId { get; set; }
        public string ParametersJson { get; set; }
    }
}
