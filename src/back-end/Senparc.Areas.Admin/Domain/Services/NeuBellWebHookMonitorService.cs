/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookMonitorService.cs
    文件功能描述：NeuBell WebHook 后台监测服务：周期性聚合各模块纽铃快照，
    交由 Dispatcher 对比差异并异步通知 WebHook；同时订阅 NeuBell 变更事件，
    条目变化后尽快触发一轮观测（不等满轮询间隔）

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 纽铃 WebHook 后台监测（IHostedService）。
/// 轮询间隔由配置节 <c>NeuBellWebHook:PollingIntervalSeconds</c> 控制（默认 30 秒，最小 5 秒）。
/// </summary>
public sealed class NeuBellWebHookMonitorService : IHostedService, IDisposable
{
    public const string SectionName = "NeuBellWebHook";

    private const int DefaultPollingIntervalSeconds = 30;
    private const int MinPollingIntervalSeconds = 5;

    private readonly IServiceProvider _serviceProvider;
    private readonly NeuBellChangeNotifier _changeNotifier;
    private readonly NeuBellWebHookDispatcher _dispatcher;
    private readonly ILogger<NeuBellWebHookMonitorService> _logger;
    private readonly CancellationTokenSource _stoppingCts = new();
    private Task _runningTask;

    public NeuBellWebHookMonitorService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        NeuBellChangeNotifier changeNotifier,
        NeuBellWebHookDispatcher dispatcher,
        ILogger<NeuBellWebHookMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _changeNotifier = changeNotifier;
        _dispatcher = dispatcher;
        _logger = logger;
        PollingInterval = TimeSpan.FromSeconds(Math.Max(
            MinPollingIntervalSeconds,
            configuration.GetValue($"{SectionName}:PollingIntervalSeconds", DefaultPollingIntervalSeconds)));
    }

    public TimeSpan PollingInterval { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runningTask = Task.Run(() => RunAsync(_stoppingCts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();
        if (_runningTask != null)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await _runningTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 停止超时，直接放弃等待
            }
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ObserveOnceAsync(stoppingToken).ConfigureAwait(false);
                await WaitNextTriggerAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // 正常停止
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NeuBell WebHook 监测循环异常退出。");
        }
    }

    /// <summary>
    /// 等待下一个触发点：轮询间隔到期，或任一 Provider 上报变更（先到先触发）
    /// </summary>
    private async Task WaitNextTriggerAsync(CancellationToken stoppingToken)
    {
        using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var delayTask = Task.Delay(PollingInterval, delayCts.Token);

        var changeTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var changeTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var providerId in _changeNotifier.SubscribeAsync(stoppingToken).ConfigureAwait(false))
                {
                    changeTcs.TrySetResult(true);
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 正常停止
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "NeuBell 变更事件订阅异常，回退为纯轮询模式。");
            }
        }, stoppingToken);

        await Task.WhenAny(delayTask, changeTask).ConfigureAwait(false);
        delayCts.Cancel();
    }

    private async Task ObserveOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var catalog = scope.ServiceProvider.GetRequiredService<NeuBellProviderCatalog>();
            var snapshotService = scope.ServiceProvider.GetRequiredService<NeuBellSnapshotService>();

            // 以固定系统身份观测，避免不同管理员的个性化视图互相污染差异基线
            var context = new NeuBellRequestContext("neubell-webhook-monitor");
            var availableProviders = await catalog.GetAvailableProvidersAsync(cancellationToken).ConfigureAwait(false);
            var expectedProviderIds = availableProviders
                .Select(provider => provider.ProviderId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var snapshots = await snapshotService.GetSnapshotsAsync(context, cancellationToken).ConfigureAwait(false);
            await _dispatcher.ObserveAsync(snapshots, expectedProviderIds, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 单轮失败不得终止监测循环
            _logger.LogWarning(ex, "NeuBell WebHook 监测轮询失败。");
        }
    }

    public void Dispose()
    {
        _stoppingCts.Dispose();
    }
}
