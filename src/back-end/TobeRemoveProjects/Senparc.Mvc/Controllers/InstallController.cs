using Senparc.Service;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Ncf.Service;
using Senparc.Core.Models.VD;

namespace Senparc.Mvc.Controllers
{
    public class InstallController : Controller
    {
        private readonly AdminUserInfoService _accountInfoService;
        private readonly SystemConfigService _systemConfigService;
        public InstallController(AdminUserInfoService accountService, SystemConfigService systemConfigService)
        {
            _accountInfoService = accountService;
            _systemConfigService = systemConfigService;
        }
        public IActionResult Index()
        {
            var adminUserInfo = _accountInfoService.Init(out string userName, out string password);

            _systemConfigService.Init();

            if (adminUserInfo == null)
            {
                return new StatusCodeResult(404);
            }

            var vd = new Install_IndexVD()
            {
                AdminUserName = userName,
                AdminPassword = password//这里不可以使用 adminUserInfo.Password，因为此参数已经是加密信息
            };
            return View(vd);
        }
    }
}
