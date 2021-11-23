using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models.DataBaseModel.Dto;
using Senparc.Ncf.Core.AppServices;
using Senparc.Service.OHS.PL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Service.OHS.AppService
{
    public class AdminUserInfoAppService : AppServiceBase
    {
        private readonly AdminUserInfoService _adminUserInfoService;

        public AdminUserInfoAppService(IServiceProvider serviceProvider, AdminUserInfoService adminUserInfoService) : base(serviceProvider)
        {
            this._adminUserInfoService = adminUserInfoService;
        }

        /// <summary>
        /// 管理员登录验证，并进行登录授权
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        [FunctionRender("管理员登录", "测试当前管理员登录", typeof(Register))]
        public async Task<object> LoginAsync([FromBody] AdminUserInfo_LoginRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AccountLoginResultDto>, AccountLoginResultDto>(async (response, logger) =>
            {
                var result = await _adminUserInfoService.LoginAsync(new Core.Models.DataBaseModel.Dto.AccountLoginDto()
                {
                    UserName = request.UserName,
                    Password = request.Password
                });

                logger.Append("管理员登录：" + request.UserName);
                logger.Append("结果：" + !result.UserName.IsNullOrEmpty());

                return result;
            }, exceptionHandler: (ex, response, logger) =>
            {
                logger.Append(ex.Message);
                logger.Append(ex.StackTrace);

                response.ErrorMessage = ex.Message;
            },
            saveLogAfterFinished: true,
            saveLogName: "管理员登录");
        }
    }
}
