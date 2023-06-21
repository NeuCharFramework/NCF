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

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [BindProperties()]
    public class LoginModel : BasePageModel/* BaseAdminPageModel*/
    {
        //[BindProperty]
        //[FromBody]
        //[Required(ErrorMessage = "ÇëÊäÈëÓÃ»§Ãû")]
        //public string Name { get; set; }

        //[BindProperty]
        //[FromBody]
        //[Required(ErrorMessage = "ÇëÊäÈëÃÜÂë")]
        //public string Password { get; set; }

        //°ó¶¨²ÎÊı
        //[BindProperty(SupportsGet = true)]
        //public string ReturnUrl { get; set; }



        private readonly AdminUserInfoService _userInfoService;
        public LoginModel(AdminUserInfoService userInfoService)
        {
            this._userInfoService = userInfoService;
        }


        public async Task<IActionResult> OnGetAsync(string ReturnUrl)
        {
            await Task.CompletedTask;
            //ÊÇ·ñÒÑ¾­µÇÂ¼
            //var logined = await base.CheckLoginedAsync(AdminAuthorizeAttribute.AuthenticationScheme);//ÅĞ¶ÏµÇÂ¼

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
        //        //errorMsg = "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º101¡£";
        //        ModelState.AddModelError(nameof(this.Password), "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º101¡£");
        //    }
        //    else if (_userInfoService.TryLogin(this.Name, this.Password, true) == null)
        //    {
        //        //errorMsg = "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º102¡£";
        //        ModelState.AddModelError(nameof(this.Password), "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º102¡£");
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

        public async Task<IActionResult> OnPostLoginAsync([FromBody]LoginInDto loginInDto/*[Required]string name,string password*/)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new { loginInDto.Name, loginInDto.Password });
            }

            var userInfo = await _userInfoService.GetUserInfo(loginInDto.Name);
            if (userInfo == null)
            {
                //errorMsg = "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º101¡£";
                return Ok("pwd", false, "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º101¡£");
                //ModelState.AddModelError(nameof(this.Password), "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º101¡£");
            }
            else if (_userInfoService.TryLogin(loginInDto.Name, loginInDto.Password, true) == null)
            {
                //errorMsg = "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º102¡£";
                //ModelState.AddModelError(nameof(this.Password), "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º102¡£");
                return Ok("pwd", false, "ÕËºÅ»òÃÜÂë´íÎó£¡´íÎó´úÂë£º102¡£");
            }

            return Ok(true);
        }

        public async Task<IActionResult> OnGetLogoutAsync(string ReturnUrl)
        {
            SenparcTrace.SendCustomLog("¹ÜÀíÔ±ÍË³öµÇÂ¼", $"ÓÃ»§Ãû£º{base.UserName}");
            await _userInfoService.LogoutAsync();
            if (string.IsNullOrEmpty(ReturnUrl))
                return RedirectToPage(new { area = "Admin" });
            else
                return LocalRedirect(ReturnUrl.UrlDecode());
        }
    }

    public class LoginInDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }
    }
}