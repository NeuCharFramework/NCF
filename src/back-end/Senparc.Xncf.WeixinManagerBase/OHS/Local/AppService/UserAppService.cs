using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.WeixinManagerBase.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerBase.OHS.Local.AppService
{
    public class UserAppService : AppServiceBase
    {
        private readonly UserService _userService;
        public UserAppService(IServiceProvider serviceProvider, UserService userService) : base(serviceProvider)
        {
            _userService = userService;
        }

        [ApiBind()]
        public async Task<AppResponseBase<string>> MyApi()
        {
            return await this.GetResponseAsync<AppResponseBase<string>, string>(async (response, logger) =>
            {
                var userList = await _userService.GetFullListAsync(z => true);

                logger.Append($"当前用户数量：{userList.Count}");
                logger.SaveLogs("UserAppService.MyApi");

                return userList.ToJson(true);
            });
        }
    }
}
