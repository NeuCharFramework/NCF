/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：StatAppService.cs
    文件功能描述：StatAppService.cs 相关实现


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

    修改标识：Senparc - 20260808
    修改描述：v0.4.0 接入 Host 实时指标统计输出

    修改标识：Senparc - 20260809
    修改描述：日志统计改为轻量流式扫描，避免整文件解析占用内存

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

    修改标识：Senparc - 20260926
    修改描述：v0.12.0 新增资源监控数据读取（后台 10 秒级记录，CO2NET 缓存最多 24 小时）

    修改标识：Senparc - 20261102
    修改描述：v0.12.3 恢复 GetHostMetrics 的 API 注册，移除资源监控方法的重复标记

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add streamed large-file Workspace uploads with tenant-aware ownership checks

----------------------------------------------------------------*/

using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    //[BackendJwtAuthorize]
    [AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class StatAppService : LocalAppServiceBase
    {
        private readonly HostMetricsCollector _hostMetricsCollector;

        public StatAppService(
            IServiceProvider serviceProvider,
            HostMetricsCollector hostMetricsCollector) : base(serviceProvider)
        {
            _hostMetricsCollector = hostMetricsCollector;
        }

        [ApiBind]
        public async Task<AppResponseBase<Stat_GetLogsResponse>> GetLogs()
        {
            return await this.GetResponseAsync<Stat_GetLogsResponse>(async (response, logger) =>
            {
                var result = new Stat_GetLogsResponse();
                var dates = SenparcTraceHelper.GetLogDate().Take(14).OrderBy(z => z).ToList();
                foreach (var date in dates)
                {
                    // 轻量扫描：只计数，不加载日志正文到内存
                    var scan = await SenparcTraceHelper.GetLogScanResultAsync(base.ServiceProvider, date);
                    result.Logs.Add(new Stat_GetLogsResponse_Item()
                    {
                        Date = date,
                        ExceptionLogCount = scan.ExceptionLogCount,
                        NormalLogCount = scan.NormalLogCount,
                        TotalLogCount = scan.TotalLogCount
                    });
                    logger.Append($"GetLogs date={date}, total={scan.TotalLogCount}, exception={scan.ExceptionLogCount}");
                }
                return result;
            });
        }

        /// <summary>
        /// 获取当天日志信息
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<Stat_GetTodayLogResponse>> GetTodayLog()
        {
            return await this.GetResponseAsync<Stat_GetTodayLogResponse>(async (response, logger) =>
            {
                var result = new Stat_GetTodayLogResponse()
                {
                    Date = SystemTime.Now.ToString("yyyyMMdd")
                };

                var todayLogPath = Path.Combine(SenparcTraceHelper.DefaultLogPath, $"SenparcTrace-{result.Date}.log");
                if (!File.Exists(todayLogPath))
                {
                    logger.Append($"GetTodayLog date={result.Date}, file missing, return empty");
                    return result;
                }

                var scan = await SenparcTraceHelper.GetLogScanResultAsync(base.ServiceProvider, result.Date);
                foreach (var item in scan.TypeCounts.OrderBy(z => z.Key.ToString()))
                {
                    result.Items.Add(new Stat_GetTodayLogResponse_Item()
                    {
                        SenparcTraceType = item.Key.ToString(),
                        Count = item.Value
                    });
                }

                logger.Append($"GetTodayLog date={result.Date}, types={result.Items.Count}, total={scan.TotalLogCount}");
                return result;
            });
        }

        /// <summary>
        /// 获取资源监控数据（后台每 10 秒记录，三个时间分档，CO2NET 缓存最多保留 24 小时）。
        /// </summary>
        [ApiBind]
        public async Task<AppResponseBase<ResourceMonitorData>> GetResourceMonitor()
        {
            return await this.GetResponseAsync<ResourceMonitorData>(async (response, logger) =>
            {
                var cache = CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();
                var data = new ResourceMonitorData
                {
                    Now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    SampleIntervalSeconds = ResourceMonitorRecorder.SampleIntervalSeconds,
                    Windows = new List<ResourceMonitorWindow>
                    {
                        await ReadResourceMonitorWindowAsync(cache, ResourceMonitorRecorder.CacheKeyFine, "fine", 10).ConfigureAwait(false),
                        await ReadResourceMonitorWindowAsync(cache, ResourceMonitorRecorder.CacheKeyMid, "mid", 60).ConfigureAwait(false),
                        await ReadResourceMonitorWindowAsync(cache, ResourceMonitorRecorder.CacheKeyCoarse, "coarse", 300).ConfigureAwait(false)
                    }
                };
                logger.Append($"GetResourceMonitor fine={data.Windows[0].Points.Count}, mid={data.Windows[1].Points.Count}, coarse={data.Windows[2].Points.Count}");
                return data;
            });
        }

        private static async Task<ResourceMonitorWindow> ReadResourceMonitorWindowAsync(
            IBaseObjectCacheStrategy cache,
            string key,
            string windowKey,
            int resolutionSeconds)
        {
            var window = await cache.GetAsync<ResourceMonitorWindow>(key).ConfigureAwait(false);
            if (window == null)
            {
                window = new ResourceMonitorWindow
                {
                    Key = windowKey,
                    ResolutionSeconds = resolutionSeconds
                };
            }
            if (window.Points == null)
            {
                window.Points = new List<ResourceMonitorPoint>();
            }
            return window;
        }

        /// <summary>
        /// 获取当前 Host 的实时 CPU、内存、网络及 Web 进程指标。
        /// </summary>
        [ApiBind]
        public async Task<AppResponseBase<HostMetricsSnapshot>> GetHostMetrics()
        {
            return await this.GetResponseAsync<HostMetricsSnapshot>((response, logger) =>
            {
                return Task.FromResult(_hostMetricsCollector.Collect());
            });
        }
    }
}
