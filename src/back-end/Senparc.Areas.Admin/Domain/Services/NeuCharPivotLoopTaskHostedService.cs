/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotLoopTaskHostedService.cs
    文件功能描述：NeuCharPivot Loop Task 调度与 NeuBell 通知


    创建标识：Senparc - 20260809

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

    修改标识：Senparc - 20260829
    修改描述：v0.7.0 新增 NeuCharPivot 全局浮动调用与工作流分析管理能力

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class NeuCharPivotNeuBellProvider : INeuBellProvider
{
    public const string ProviderName = "neucharpivot.loop";
    private const int Capacity = 30;
    private readonly ConcurrentQueue<NeuBellItem> _items = new();

    public string ProviderId => ProviderName;
    public string ModuleUid => Register.ModuleUid;

    public void AddResult(int taskId, string functionName, bool succeeded, DateTimeOffset completedAt)
    {
        _items.Enqueue(new NeuBellItem(
            $"loop-{taskId}-{completedAt.ToUnixTimeMilliseconds()}",
            succeeded ? "Loop Task 执行成功" : "Loop Task 执行失败",
            functionName,
            1,
            succeeded ? "success" : "danger",
            "/Admin/NeuCharPivot/Aggregate",
            completedAt));
        while (_items.Count > Capacity && _items.TryDequeue(out _)) { }
    }

    public int ConsumeAll()
    {
        var count = 0;
        while (_items.TryDequeue(out _))
        {
            count++;
        }
        return count;
    }

    public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var items = _items.Reverse().ToList();
        return ValueTask.FromResult(new NeuBellSnapshot(
            ProviderId,
            ModuleUid,
            "NeuCharPivot Loop Task",
            "fa fa-repeat",
            true,
            items));
    }
}

public sealed class NeuCharPivotLoopTaskHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NeuCharPivotLoopTaskHostedService> _logger;

    public NeuCharPivotLoopTaskHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<NeuCharPivotLoopTaskHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunDueTasksAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描 NeuCharPivot Loop Task 失败，将在下一个周期重试。");
            }
        }
    }

    private async Task RunDueTasksAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var loopTaskService = scope.ServiceProvider.GetRequiredService<NeuCharPivotLoopTaskService>();
        var functionEntityService = scope.ServiceProvider.GetRequiredService<NeuCharPivotFunctionService>();
        var functionService = scope.ServiceProvider.GetRequiredService<NeuCharFunctionService>();
        var logService = scope.ServiceProvider.GetRequiredService<NeuCharExecutionLogService>();
        var parameterProtector = scope.ServiceProvider.GetRequiredService<NeuCharParameterProtector>();
        var neuBellProvider = scope.ServiceProvider.GetRequiredService<NeuCharPivotNeuBellProvider>();
        var neuBellPublisher = scope.ServiceProvider.GetService<INeuBellPublisher>();
        var workflowProvider = scope.ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
        var now = DateTime.UtcNow;
        var dueTasks = await loopTaskService.GetFullListAsync(
            z => z.Enabled && z.NextRunAt != null && z.NextRunAt <= now,
            z => z.NextRunAt,
            OrderingType.Ascending).ConfigureAwait(false);

        foreach (var task in dueTasks.Take(20))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var function = await functionEntityService.GetObjectAsync(z => z.Id == task.FunctionId)
                .ConfigureAwait(false);
            if (function == null)
            {
                const string error = "关联的 NeuCharPivot Function 已不存在，Loop Task 已自动停用。";
                task.MarkStarted();
                task.DisableForError(error);
                await loopTaskService.SaveObjectAsync(task).ConfigureAwait(false);
                var missingLog = new NeuCharExecutionLog(
                    "loop-task",
                    task.Id,
                    null,
                    null,
                    $"Function #{task.FunctionId}",
                    $"loop-{task.Id}-{Guid.NewGuid():N}");
                missingLog.Complete(false, null, error);
                await logService.SaveObjectAsync(missingLog).ConfigureAwait(false);
                if (task.UseNeuBell)
                {
                    neuBellProvider.AddResult(task.Id, $"Function #{task.FunctionId}", false, DateTimeOffset.UtcNow);
                    if (neuBellPublisher != null)
                    {
                        await neuBellPublisher.NotifyChangedAsync(
                            NeuCharPivotNeuBellProvider.ProviderName,
                            cancellationToken).ConfigureAwait(false);
                    }
                }
                continue;
            }

            // 先推进 NextRunAt，避免一次执行超过轮询间隔时被同一进程重复领取。
            task.MarkStarted();
            await loopTaskService.SaveObjectAsync(task).ConfigureAwait(false);

            var correlationId = $"loop-{task.Id}-{Guid.NewGuid():N}";
            var log = new NeuCharExecutionLog(
                "loop-task",
                task.Id,
                function.ModuleUid,
                function.FunctionKey,
                function.FunctionName,
                correlationId);
            await logService.SaveObjectAsync(log).ConfigureAwait(false);

            try
            {
                var executionParameters = parameterProtector.Unprotect(task.ParametersJson);
                var result = await functionService.ExecuteAsync(
                    function.ModuleUid,
                    function.FunctionKey,
                    executionParameters,
                    cancellationToken).ConfigureAwait(false);
                var resultText = result.Data?.ToString();
                task.MarkCompleted(result.Success, result.ErrorMessage);
                log.Complete(result.Success, resultText, result.ErrorMessage);
                await loopTaskService.SaveObjectAsync(task).ConfigureAwait(false);
                await logService.SaveObjectAsync(log).ConfigureAwait(false);
                await TriggerWorkflowAsync(
                    task,
                    function,
                    result.Success,
                    resultText,
                    result.ErrorMessage,
                    logService,
                    workflowProvider,
                    cancellationToken).ConfigureAwait(false);

                if (task.UseNeuBell)
                {
                    neuBellProvider.AddResult(task.Id, function.FunctionName, result.Success, DateTimeOffset.UtcNow);
                    if (neuBellPublisher != null)
                    {
                        await neuBellPublisher.NotifyChangedAsync(
                            NeuCharPivotNeuBellProvider.ProviderName,
                            cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                task.MarkCompleted(false, ex.Message);
                log.Complete(false, null, ex.ToString());
                await loopTaskService.SaveObjectAsync(task).ConfigureAwait(false);
                await logService.SaveObjectAsync(log).ConfigureAwait(false);
                await TriggerWorkflowAsync(
                    task,
                    function,
                    false,
                    null,
                    ex.Message,
                    logService,
                    workflowProvider,
                    cancellationToken).ConfigureAwait(false);
                if (task.UseNeuBell)
                {
                    neuBellProvider.AddResult(task.Id, function.FunctionName, false, DateTimeOffset.UtcNow);
                    if (neuBellPublisher != null)
                    {
                        await neuBellPublisher.NotifyChangedAsync(
                            NeuCharPivotNeuBellProvider.ProviderName,
                            cancellationToken).ConfigureAwait(false);
                    }
                }
                _logger.LogError(ex, "Loop Task 执行异常：TaskId={TaskId}", task.Id);
            }
        }
    }

    private static async Task TriggerWorkflowAsync(
        NeuCharPivotLoopTask task,
        NeuCharPivotFunction function,
        bool succeeded,
        string result,
        string error,
        NeuCharExecutionLogService logService,
        IWorkflowFunctionCallingProvider workflowProvider,
        CancellationToken cancellationToken)
    {
        if (!task.WorkflowId.HasValue)
        {
            return;
        }

        var workflowLog = new NeuCharExecutionLog(
            "loop-workflow",
            task.Id,
            function.ModuleUid,
            task.WorkflowId.Value.ToString(),
            $"Workflow #{task.WorkflowId.Value}",
            $"loop-workflow-{task.Id}-{Guid.NewGuid():N}");
        await logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);

        WorkflowFunctionCallingResult workflowResult;
        try
        {
            if (workflowProvider == null)
            {
                workflowResult = new WorkflowFunctionCallingResult(
                    false,
                    null,
                    "NeuChar Workflow 模块未安装或未开启。");
            }
            else
            {
                var input = JsonSerializer.Serialize(new
                {
                    taskId = task.Id,
                    functionId = function.Id,
                    functionKey = function.FunctionKey,
                    functionName = function.FunctionName,
                    succeeded,
                    data = result,
                    error,
                    completedAt = DateTimeOffset.UtcNow
                });
                workflowResult = await workflowProvider.ExecuteAsync(
                    task.WorkflowId.Value,
                    task.AdminUserId,
                    input,
                    null,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            workflowResult = new WorkflowFunctionCallingResult(false, null, ex.Message);
        }

        workflowLog.Complete(
            workflowResult.Success,
            workflowResult.Output,
            workflowResult.ErrorMessage);
        await logService.SaveObjectAsync(workflowLog).ConfigureAwait(false);
    }
}
