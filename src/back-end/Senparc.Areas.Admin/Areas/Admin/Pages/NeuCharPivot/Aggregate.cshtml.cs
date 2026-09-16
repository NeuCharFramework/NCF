/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Aggregate.cshtml.cs
    文件功能描述：集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

    修改标识：Senparc - 20260829
    修改描述：v0.7.0 新增 NeuCharPivot 全局浮动调用与工作流分析管理能力

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;

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
    IAdminWorkContextProvider adminWorkContextProvider,
    IEnumerable<IWorkflowFunctionCallingProvider> workflowProviders) : BaseAdminPageModel(serviceProvider)
{
    private readonly IWorkflowFunctionCallingProvider _workflowProvider = workflowProviders?.FirstOrDefault();
    private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

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
        var workflowOptions = await GetWorkflowOptionsAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        var executionLogs = await logService.GetFullListAsync(z => true).ConfigureAwait(false);
        var runningLoopTaskIds = executionLogs
            .Where(log => log.SourceType == "loop-task" && log.FinishedAt == null)
            .Select(log => log.SourceId)
            .ToHashSet();
        var now = DateTime.UtcNow;
        var modules = snapshots.Select(snapshot => new
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
            layoutSchemaJson = snapshot.Configuration.LayoutSchemaJson,
            snapshot.ModuleAvailable,
            snapshot.ModuleState,
            functions = snapshot.Functions.Where(z => z.Visible).Select(function => new
            {
                function.Id,
                function.FunctionKey,
                function.FunctionName,
                function.Description,
                parameterSchemaJson = pivotService.GetCurrentParameterSchemaJson(
                    function,
                    snapshot.FunctionCatalog),
                function.DefaultParametersJson,
                function.ModuleVersion,
                available = snapshot.FunctionAvailability.TryGetValue(function.Id, out var available) && available,
                loopTask = snapshot.LoopTasks.TryGetValue(function.Id, out var task) ? new
                {
                    task.Enabled,
                    task.IntervalSeconds,
                    task.UseNeuBell,
                    task.WorkflowId,
                    isRunning = runningLoopTaskIds.Contains(task.Id),
                    status = GetLoopTaskStatus(task, runningLoopTaskIds.Contains(task.Id), now),
                    task.NextRunAt,
                    task.LastRunAt,
                    task.LastSucceeded,
                    task.LastError
                } : null
            })
        }).ToList();

        var functions = modules.SelectMany(module => module.functions).ToList();
        var loopTasks = functions
            .Where(function => function.loopTask != null)
            .Select(function => function.loopTask)
            .ToList();
        var loopLogs = executionLogs.Where(log => log.SourceType == "loop-task").ToList();
        var workflowLogs = executionLogs.Where(log => log.SourceType == "loop-workflow").ToList();
        var pivotLogs = executionLogs.Where(log => log.SourceType == "pivot").ToList();
        var workflowNames = workflowOptions.ToDictionary(z => z.Id, z => z.Name);

        return Ok(new
        {
            modules,
            workflowOptions,
            generatedAt = DateTimeOffset.UtcNow,
            summary = new
            {
                moduleCount = modules.Count,
                availableModuleCount = modules.Count(module => module.ModuleAvailable),
                unavailableModuleCount = modules.Count(module => !module.ModuleAvailable),
                functionCount = functions.Count,
                availableFunctionCount = functions.Count(function => function.available),
                unavailableFunctionCount = functions.Count(function => !function.available),
                loopTaskCount = loopTasks.Count,
                enabledLoopTaskCount = loopTasks.Count(task => task.Enabled),
                disabledLoopTaskCount = loopTasks.Count(task => !task.Enabled),
                runningLoopTaskCount = loopTasks.Count(task => task.isRunning),
                waitingLoopTaskCount = loopTasks.Count(task => task.status == "countdown"),
                failedLoopTaskCount = loopTasks.Count(task => task.LastSucceeded == false),
                workflowLoopTaskCount = loopTasks.Count(task => task.WorkflowId.HasValue),
                unresolvedWorkflowLoopTaskCount = loopTasks.Count(task =>
                    task.WorkflowId.HasValue && !workflowNames.ContainsKey(task.WorkflowId.Value)),
                workflowCount = workflowOptions.Count,
                pivotExecutionCount = pivotLogs.Count,
                pivotRunningCount = pivotLogs.Count(log => log.FinishedAt == null),
                pivotSuccessCount = pivotLogs.Count(log => log.FinishedAt != null && log.Succeeded == true),
                pivotFailedCount = pivotLogs.Count(log => log.FinishedAt != null && log.Succeeded != true),
                loopExecutionCount = loopLogs.Count,
                loopRunningCount = loopLogs.Count(log => log.FinishedAt == null),
                loopSuccessCount = loopLogs.Count(log => log.FinishedAt != null && log.Succeeded == true),
                loopFailedCount = loopLogs.Count(log => log.FinishedAt != null && log.Succeeded != true),
                workflowTriggerCount = workflowLogs.Count,
                workflowTriggerRunningCount = workflowLogs.Count(log => log.FinishedAt == null),
                workflowTriggerSuccessCount = workflowLogs.Count(log => log.FinishedAt != null && log.Succeeded == true),
                workflowTriggerFailedCount = workflowLogs.Count(log => log.FinishedAt != null && log.Succeeded != true)
            }
        });
    }

    private async Task<IReadOnlyList<NeuCharPivotWorkflowOption>> GetWorkflowOptionsAsync(
        CancellationToken cancellationToken)
    {
        if (_workflowProvider == null)
        {
            return Array.Empty<NeuCharPivotWorkflowOption>();
        }

        var adminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
        var workflows = await _workflowProvider.GetAvailableAsync(adminUserId, cancellationToken)
            .ConfigureAwait(false);
        return workflows
            .Select(workflow => new NeuCharPivotWorkflowOption(
                workflow.Id,
                workflow.Name,
                workflow.Description ?? string.Empty,
                workflow.Parameters?.Select(parameter => parameter.Name).ToList()
                    ?? new List<string>()))
            .ToList();
    }

    private static string GetLoopTaskStatus(
        NeuCharPivotLoopTask task,
        bool isRunning,
        DateTime now)
    {
        if (isRunning)
        {
            return "running";
        }
        if (!task.Enabled)
        {
            return "disabled";
        }
        if (task.LastSucceeded == false)
        {
            return "failed";
        }
        if (task.NextRunAt.HasValue && task.NextRunAt.Value > now)
        {
            return "countdown";
        }
        return "due";
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
