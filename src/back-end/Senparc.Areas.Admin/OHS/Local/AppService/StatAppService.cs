using Senparc.Areas.Admin.Domain.Dto;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.CO2NET;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    //[BackendJwtAuthorize]
    [AdminAuthorize("AdminOnly")]
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
                var dates = SenparcTraceHelper.GetLogDate().Take(14).ToList();
                foreach (var date in dates)
                {
                    var traceItemList = SenparcTraceHelper.GetAllLogs(date);
                    result.Logs.Add(new Stat_GetLogsResponse_Item()
                    {
                        Date = date,
                        LogCount = traceItemList.Count
                    });
                }
                return result;
            });
        }
    }
}
