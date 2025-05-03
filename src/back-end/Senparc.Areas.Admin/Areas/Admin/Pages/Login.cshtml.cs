using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Models;
using System;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.Tenant.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [BindProperties()]
    public class LoginModel : BasePageModel/* BaseAdminPageModel*/
    {
        //[BindProperty]
        //[FromBody]
        //[Required(ErrorMessage = "请输入用户名")]
        //public string Name { get; set; }

        //[BindProperty]
        //[FromBody]
        //[Required(ErrorMessage = "请输入密码")]
        //public string Password { get; set; }

        //绑定参数
        //[BindProperty(SupportsGet = true)]
        //public string ReturnUrl { get; set; }



        private readonly AdminUserInfoService _userInfoService;
        private readonly TenantInfoService _tenantInfoService;
        public LoginModel(AdminUserInfoService userInfoService, TenantInfoService tenantInfoService)
        {
            this._userInfoService = userInfoService;
            this._tenantInfoService = tenantInfoService;
        }


        public async Task<IActionResult> OnGetAsync(string ReturnUrl)
        {
            await Task.CompletedTask;
            //是否已经登录
            //var logined = await base.CheckLoginedAsync(AdminAuthorizeAttribute.AuthenticationScheme);//判断登录

            //if (logined)
            //{
            //    if (ReturnUrl.IsNullOrEmpty())
            //    {
            //        return RedirectToPage("/Index");
            //    }
            //    return LocalRedirect(ReturnUrl.UrlDecode());
            //}

            return null;
        }

        //public async Task<IActionResult> OnPostAsync(/*[Required]string name,string password*/)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return null;
        //    }
        //    string errorMsg = null;

        //    var userInfo = await _userInfoService.GetUserInfo(this.Name);
        //    if (userInfo == null)
        //    {
        //        //errorMsg = "账号或密码错误！错误代码：101。";
        //        ModelState.AddModelError(nameof(this.Password), "账号或密码错误！错误代码：101。");
        //    }
        //    else if (_userInfoService.TryLogin(this.Name, this.Password, true) == null)
        //    {
        //        //errorMsg = "账号或密码错误！错误代码：102。";
        //        ModelState.AddModelError(nameof(this.Password), "账号或密码错误！错误代码：102。");
        //    }

        //    if (!errorMsg.IsNullOrEmpty() || !ModelState.IsValid)
        //    {
        //        //this.MessagerList = new List<Messager>
        //        //{
        //        //    new Messager(Senparc.Ncf.Core.Enums.MessageType.danger, errorMsg)
        //        //};
        //        return null;
        //    }

        //    if (this.ReturnUrl.IsNullOrEmpty())
        //    {
        //        return RedirectToPage("/Index");
        //    }
        //    return LocalRedirect(this.ReturnUrl.UrlDecode());
        //}

        public async Task<IActionResult> OnPostLoginAsync([FromBody] LoginInDto loginInDto/*[Required]string name,string password*/)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new { loginInDto.Name, loginInDto.Password });
            }

            AdminUserInfo userInfo = null;
            string tenantKey = loginInDto.Tenant;
            try
            {
                if (SiteConfig.SenparcCoreSetting.EnableMultiTenant && !tenantKey.IsNullOrEmpty())
                {
                    var tenantInfo = await this._tenantInfoService.GetObjectAsync(z => z.TenantKey.ToUpper() == tenantKey.ToUpper());
                    if (tenantInfo == null)
                    {
                        SenparcTrace.SendCustomLog("登录失败", $", 错误：租户名称错误：" + tenantKey);
                        return Ok("pwd", false, $"用户名：{loginInDto.Name}, 错误：账号或密码错误！");
                    }

                    var requestTenantInfo = this._tenantInfoService.GetRequestTenantInfo(tenantInfo);
                    if (!_userInfoService.SetTenantInfo(requestTenantInfo))
                    {
                        SenparcTrace.SendCustomLog("租户配置失败", $", 错误：租户名配置错误：" + tenantKey);
                        return Ok("pwd", false, $"用户名：{loginInDto.Name}, 错误：账号或密码错误！");
                    }

                    tenantKey = tenantInfo.TenantKey;
                }

                userInfo = await _userInfoService.GetUserInfoAsync(loginInDto.Name);
            }
            catch (Exception ex)
            {
                return Ok("pwd", false, ex.Message);
            }

            if (userInfo == null)
            {
                SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：账号或密码错误！101");
                return Ok("pwd", false, "账号或密码错误！");
                //ModelState.AddModelError(nameof(this.Password), "账号或密码错误！");
            }

            try
            {
                //TODO 需要把 userInfo 获取过程封装到下面的方法中，统一处理账号锁定
                if (await _userInfoService.TryLoginAsync(userInfo, loginInDto.Password, true, tenantKey) == null)
                {
                    //ModelState.AddModelError(nameof(this.Password), "账号或密码错误！");
                    SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：账号或密码错误！102");
                    return Ok("pwd", false, "账号或密码错误！");
                }

                return Ok(true);
            }
            catch (LoginLockException ex)
            {
                SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：{ex.Message}");
                return Ok("pwd", false, ex.Message);
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：{ex.Message}");
                //其他异常，不返回错误信息
                return Ok("pwd", false, "账号或密码错误！");
            }
        }

        public async Task<IActionResult> OnGetLogoutAsync(string ReturnUrl)
        {
            SenparcTrace.SendCustomLog("管理员退出登录", $"用户名：{base.UserName}");
            await _userInfoService.LogoutAsync();
            if (string.IsNullOrEmpty(ReturnUrl))
            {
                return RedirectToPage(new { area = "Admin" });
            }
            else
            {
                return LocalRedirect(ReturnUrl.UrlDecode());
            }
        }

        public IActionResult OnGetCheckMultiTenant()
        {
            return Ok(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }
    }

    public class LoginInDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }
        public string Tenant { get; set; }
    }
}