/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ResourceMonitorRecorder.cs
    文件功能描述：后台资源监控记录器：每 10 秒采样一次 Host/Web 进程指标，
    以“10 秒 / 1 分钟 / 5 分钟”三个时间分档写入统一的 CO2NET 对象缓存
    （本地内存或 Redis 取决于站点配置），最多保留 24 小时，供 Admin 首页
    Host 资源监控图表随时读取——UI 未打开时记录依然持续。
    性能开销评估（实现前提）：
      - 采样：每次仅读取 /proc 或系统 P/Invoke 计数器（<1ms），平均 CPU < 0.01%
      - 缓存写入：约 7 次小对象写入/分钟（单次 5-30KB）
      - 内存：三个窗口合计 < 70KB
      - 异常隔离：任何采样/缓存异常只记录日志，绝不影响主业务

    创建标识：Senparc - 20260926
    修改描述：v0.12.0 新增资源监控后台记录器

    修改标识：Senparc - 20261102
    修改描述：v0.12.7 更新合并后资源监控图表的说明

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add PromptRange selector service, UI components, and tests

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 后台资源监控记录器（10 秒采样，三档时间分档，CO2NET 缓存最多保留 24 小时）。
    /// </summary>
    public sealed class ResourceMonitorRecorder : BackgroundService
    {
        public const string CacheKeyFine = "NcfResourceMonitor:fine";
        public const string CacheKeyMid = "NcfResourceMonitor:mid";
        public const string CacheKeyCoarse = "NcfResourceMonitor:coarse";

        /// <summary>采样间隔（秒）。</summary>
        public const int SampleIntervalSeconds = 10;
        /// <summary>10 秒档最多保留 180 点（30 分钟）。</summary>
        public const int FineMaxPoints = 180;
        /// <summary>1 分钟档最多保留 360 点（6 小时）。</summary>
        public const int MidMaxPoints = 360;
        /// <summary>5 分钟档最多保留 288 点（24 小时）。</summary>
        public const int CoarseMaxPoints = 288;

        private static readonly TimeSpan FineTtl = TimeSpan.FromMinutes(40);
        private static readonly TimeSpan MidTtl = TimeSpan.FromHours(7);
        private static readonly TimeSpan CoarseTtl = TimeSpan.FromHours(25);

        private readonly IBaseObjectCacheStrategy _cache;
        private readonly ILogger<ResourceMonitorRecorder> _logger;

        // 独立采样实例：不与 Admin 首页 2 秒轮询共享差分数值基准，
        // 保证 10 秒档的 CPU 百分比始终基于 10 秒区间计算。
        private readonly HostMetricsCollector _collector = new();

        // 仅保留聚合所需的最少数据（当前分钟 + 最近 6 个 1 分钟点），内存占用极小。
        private readonly List<ResourceMonitorPoint> _pendingMinute = new();
        private readonly List<ResourceMonitorPoint> _recentMinutes = new();
        private long _currentMinuteIndex;
        private long _currentFiveMinuteIndex;

        public ResourceMonitorRecorder(ILogger<ResourceMonitorRecorder> logger)
        {
            _logger = logger;
            _cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 先采集一帧作为 CPU/网络差分的基准，再进入周期采样
            try
            {
                _collector.Collect();
                await Task.Delay(TimeSpan.FromSeconds(SampleIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(SampleIntervalSeconds));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        await RecordTickAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        // 记录器是纯后台增强功能：任何异常都不允许影响主业务
                        _logger.LogWarning(ex, "资源监控采样失败，等待下一个周期。");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停机
            }
        }

        private async Task RecordTickAsync()
        {
            var snapshot = _collector.Collect();
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var point = new ResourceMonitorPoint
            {
                T = nowMs,
                Cpu = snapshot.CpuUsagePercent,
                Pcpu = snapshot.ProcessCpuUsagePercent,
                Mem = snapshot.MemoryUsagePercent,
                Pmem = snapshot.MemoryTotalBytes > 0
                    ? Math.Round(snapshot.ProcessWorkingSetBytes * 100d / snapshot.MemoryTotalBytes, 2)
                    : (double?)null,
                Rx = snapshot.NetworkReceiveBytesPerSecond.HasValue
                    ? Math.Round(snapshot.NetworkReceiveBytesPerSecond.Value * 8d / 1000000d, 3)
                    : (double?)null,
                Tx = snapshot.NetworkSendBytesPerSecond.HasValue
                    ? Math.Round(snapshot.NetworkSendBytesPerSecond.Value * 8d / 1000000d, 3)
                    : (double?)null
            };

            var minuteIndex = nowMs / 60000;
            var fiveMinuteIndex = nowMs / 300000;

            if (_currentMinuteIndex != 0 && minuteIndex != _currentMinuteIndex)
            {
                await FinalizeMinuteAsync().ConfigureAwait(false);
            }

            if (_currentFiveMinuteIndex != 0 && fiveMinuteIndex != _currentFiveMinuteIndex)
            {
                await FinalizeFiveMinutesAsync().ConfigureAwait(false);
            }

            _currentMinuteIndex = minuteIndex;
            _currentFiveMinuteIndex = fiveMinuteIndex;

            _pendingMinute.Add(point);
            await AppendWindowAsync(
                CacheKeyFine, "fine", 10, point, FineMaxPoints, FineTtl).ConfigureAwait(false);
        }

        private async Task FinalizeMinuteAsync()
        {
            if (_pendingMinute.Count == 0)
            {
                return;
            }

            var minutePoint = Aggregate(_pendingMinute, _pendingMinute[0].T / 60000 * 60000);
            _pendingMinute.Clear();

            _recentMinutes.Add(minutePoint);
            if (_recentMinutes.Count > 6)
            {
                _recentMinutes.RemoveRange(0, _recentMinutes.Count - 6);
            }

            await AppendWindowAsync(
                CacheKeyMid, "mid", 60, minutePoint, MidMaxPoints, MidTtl).ConfigureAwait(false);
        }

        private async Task FinalizeFiveMinutesAsync()
        {
            if (_recentMinutes.Count == 0)
            {
                return;
            }

            var window = _recentMinutes.TakeLast(5).ToList();
            var fiveMinutePoint = Aggregate(window, window[0].T / 300000 * 300000);
            await AppendWindowAsync(
                CacheKeyCoarse, "coarse", 300, fiveMinutePoint, CoarseMaxPoints, CoarseTtl).ConfigureAwait(false);
        }

        private static ResourceMonitorPoint Aggregate(
            List<ResourceMonitorPoint> source, long timestamp)
        {
            return new ResourceMonitorPoint
            {
                T = timestamp,
                Cpu = Average(source, point => point.Cpu),
                Pcpu = Average(source, point => point.Pcpu),
                Mem = Average(source, point => point.Mem),
                Pmem = Average(source, point => point.Pmem),
                Rx = Average(source, point => point.Rx),
                Tx = Average(source, point => point.Tx)
            };
        }

        private static double? Average(
            List<ResourceMonitorPoint> source,
            Func<ResourceMonitorPoint, double?> selector)
        {
            double sum = 0;
            var count = 0;
            foreach (var point in source)
            {
                var value = selector(point);
                if (value.HasValue)
                {
                    sum += value.Value;
                    count++;
                }
            }
            return count > 0 ? Math.Round(sum / count, 2) : (double?)null;
        }

        /// <summary>
        /// 读取-追加-截断-回写单个分档窗口。记录器是唯一写者（单后台循环），无并发写风险。
        /// </summary>
        private async Task AppendWindowAsync(
            string key,
            string windowKey,
            int resolutionSeconds,
            ResourceMonitorPoint point,
            int maxPoints,
            TimeSpan ttl)
        {
            var window = await _cache.GetAsync<ResourceMonitorWindow>(key).ConfigureAwait(false);
            if (window == null)
            {
                window = new ResourceMonitorWindow
                {
                    Key = windowKey,
                    ResolutionSeconds = resolutionSeconds
                };
            }
            else if (window.Points == null)
            {
                window.Points = new List<ResourceMonitorPoint>();
            }

            if (window.Points.Count == 0 || point.T > window.Points[window.Points.Count - 1].T)
            {
                window.Points.Add(point);
            }

            if (window.Points.Count > maxPoints)
            {
                window.Points.RemoveRange(0, window.Points.Count - maxPoints);
            }

            await _cache.SetAsync(key, window, ttl).ConfigureAwait(false);
        }
    }
}
