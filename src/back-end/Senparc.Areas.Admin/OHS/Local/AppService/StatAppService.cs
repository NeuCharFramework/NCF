/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：StatAppService.cs
    文件功能描述：StatAppService.cs 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/

using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.CO2NET;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    //[BackendJwtAuthorize]
    [AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class StatAppService : LocalAppServiceBase
    {
        public StatAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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
                    var traceItemList = await SenparcTraceHelper.GetAllLogsAsync(base.ServiceProvider, date);
                    var exceptionCount = traceItemList.Count(z => z.IsException);
                    result.Logs.Add(new Stat_GetLogsResponse_Item()
                    {
                        Date = date,
                        ExceptionLogCount = exceptionCount,
                        NormalLogCount = traceItemList.Count - exceptionCount,
                        TotalLogCount = traceItemList.Count
                    });
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

                var dateLog = await SenparcTraceHelper.GetAllLogsAsync(base.ServiceProvider, result.Date);
                var groupedLogs = dateLog.GroupBy(z => z.SenparcTraceType);
                foreach (var item in groupedLogs)
                {
                    result.Items.Add(new Stat_GetTodayLogResponse_Item()
                    {
                        SenparcTraceType = item.Key.ToString(),
                        Count = item.Count()
                    });
                }

                return result;
            });
        }
    }
}
