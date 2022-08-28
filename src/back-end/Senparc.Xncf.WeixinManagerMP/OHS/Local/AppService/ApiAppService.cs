using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.WeixinManagerBase.OHS.Local.PL;
using System;
using System.Threading.Tasks;
using System.Linq;
using Senparc.Xncf.WeixinManagerMP.Domain.Services;

namespace Senparc.Xncf.WeixinManagerMP.OHS.Local.AppService
{
    public class ApiAppService : AppServiceBase
    {
        private readonly MpUserService _mpUserService;
        public ApiAppService(IServiceProvider serviceProvider, MpUserService mpUserService) : base(serviceProvider)
        {
            _mpUserService = mpUserService;
        }

        /*
         * 使用 [ApiBind] 可将任意方法或类快速创建动态 WebApi。
         * 在 DDD 系统中，出于安全和防腐考虑，建议只在 AppService 上使用。
         * 当 AppService 上添加 [ApiBind] 标签满足不了需求时，仍然可以手动创建 ApiController。
         */

        /// <summary>
        /// 将 AppService 暴露为 WebApi
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<string>> MyApi()
        {
            return await this.GetResponseAsync<AppResponseBase<string>, string>(async (response, logger) =>
            {
                var weixinSettting = Senparc.Weixin.Config.SenparcWeixinSetting;
                var appId = weixinSettting.WeixinAppId;
                var openId = new[] {

                    "oxRg0uLsnpHjb8o93uVnwMK_WAVw",
                    "oxRg0uJ7qwdfktPIWsRH4jIsdVMU",
                    "oxRg0uLyWVHB1QRVohAEVXNlqDvw",
                    "oxRg0uAWjbHmpSSE4N90khs614ZA"
                    };

                openId.AsParallel().ForAll(async openId =>
                {
                    var userInfo = await Senparc.Weixin.MP.AdvancedAPIs.UserApi.InfoAsync(appId, openId);

                    await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(appId, openId, $"这是一条来自 NCF 直播的消息。时间：{SystemTime.Now}。你的关注信息：{userInfo.subscribe}");
                });

                return weixinSettting.MpSetting.WeixinAppId;
            });
        }

        public async Task<StringAppResponse> SyncUser()
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var dt = SystemTime.Now;
                var changedCount = await _mpUserService.SyncMpUser();
                return $"总共更新了 {changedCount} 条数据，总耗时：{SystemTime.DiffTotalMS(dt)} 毫秒";
            });
        }

        /// <summary>
        /// 自定义 Post 类型和复杂参数，同时测试异常抛出和自定义状态码
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse 是 AppResponseBase<string> 的快捷写法
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                throw new NcfExceptionBase($"抛出异常测试，传输参数：{request.FirstName} {request.LastName}");
                response.StateCode = 100;
            },
            exceptionHandler: (ex, response, logger) =>
            {
                logger.Append($"正在处理异常，信息：{ex.Message}");
            },
            afterFunc: (response, logger) =>
            {
                if (response.Success != true)
                {
                    response.StateCode = 101;
                }
            });
        }
    }
}
