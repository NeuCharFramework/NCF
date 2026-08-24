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

----------------------------------------------------------------*/

using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.CO2NET;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using System;
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
