using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin.Entities;

namespace Senparc.Xncf.WeixinManagerBase.OHS.Local.AppService
{
    public class WeixinRegisterService : AppServiceBase
    {
        internal static List<Func<IRegisterService, SenparcWeixinSetting, int>> WeixinRegisterList = new List<Func<IRegisterService, SenparcWeixinSetting, int>>();


        public WeixinRegisterService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 注册微信模块
        /// </summary>
        /// <returns></returns>
        //[ApiBind]
        public async Task<AppResponseBase<int>> RegisterWeixin(Func<IRegisterService, SenparcWeixinSetting, int> registerFunc)
        {
            return await this.GetResponseAsync<AppResponseBase<int>, int>(async (response, logger) =>
            {
                WeixinRegisterList.Add(registerFunc);
                return 200;
            });
        }
    }
}
