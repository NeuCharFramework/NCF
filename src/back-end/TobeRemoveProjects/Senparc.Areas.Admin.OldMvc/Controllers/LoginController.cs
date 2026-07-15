using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Senparc.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using Models.VD;
    using Senparc.Ncf.Core.Models;
    using Microsoft.AspNetCore.Mvc;
    using Senparc.Ncf.Core.Extensions;
    using Senparc.CO2NET.Extensions;
    using Senparc.Ncf.Service;

    [AllowAnonymous]
    public class LoginController : BaseAdminController
    {
        private readonly AdminUserInfoService _userInfoService;
        public LoginController(AdminUserInfoService userInfoService)
        {
            this._userInfoService = userInfoService;
        }

        public async Task<IActionResult> Index(string returnUrl)
        {
            //是否已经登录
            var authenticate = await HttpContext.AuthenticateAsync(AdminAuthorizeAttribute.AuthenticationScheme);

            if (authenticate.Succeeded)
            {
                if (returnUrl.IsNullOrEmpty())
                {
                    return RedirectToAction("Index", "Home");
                }
                return Redirect(returnUrl.UrlDecode());
            }
            Login_IndexVD vd = new Login_IndexVD
            {
                ReturnUrl = returnUrl,
                MessagerList = new List<Messager>(),
                IsLogined = this.HttpContext.User.Identity.IsAuthenticated,
            };
          
            return View(vd);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Index(Login_IndexVD vdForm)
        {
            if (!ModelState.IsValid)
            {
                return View(vdForm);
            }
            string errorMsg = null;

            var userInfo = _userInfoService.GetUserInfo(vdForm.Name);
            if (userInfo == null)
            {
                errorMsg = "账号或密码错误！错误代码：101。";
            }
            else if (_userInfoService.TryLogin(vdForm.Name, vdForm.Password, true) == null)
            {
                errorMsg = "账号或密码错误！错误代码：102。";
            }

            if (!errorMsg.IsNullOrEmpty() || !ModelState.IsValid)
            {
                vdForm.MessagerList = new List<Messager>
                {
                    new Messager(Senparc.Ncf.Core.Enums.MessageType.danger, errorMsg)
                };
                return View(vdForm);
            }

            if (vdForm.ReturnUrl.IsNullOrEmpty())
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(vdForm.ReturnUrl.UrlDecode());
        }

        public ActionResult Logout()
        {
            _userInfoService.Logout();
            return RedirectToAction("Index");
        }
    }
}
