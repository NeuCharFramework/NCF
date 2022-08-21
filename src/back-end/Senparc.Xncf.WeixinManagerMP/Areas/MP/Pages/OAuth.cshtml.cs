using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.CO2NET.Extensions;
using Senparc.Weixin;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Xncf.WeixinManagerBase.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManagerBase.Domain.Services;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerMP.Areas.MP.Pages
{
    public class OAuthModel : PageModel
    {
        //下面换成账号对应的信息，也可以放入web.config等地方方便配置和更换
        public readonly string appId = Config.SenparcWeixinSetting.WeixinAppId;//与微信公众账号后台的AppId设置保持一致，区分大小写。
        private readonly string appSecret = Config.SenparcWeixinSetting.WeixinAppSecret;//与微信公众账号后台的AppId设置保持一致，区分大小写。

        private UserService _userService;
        private IHttpContextAccessor _httpContextAccessor;

        public User_ViewDto User_ViewDto { get; set; }

        public OAuthModel(UserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnGet()
        {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var user = _userService.GetObject(z => z.Id == userId.Value);
                User_ViewDto = _userService.Mapper.Map<User_ViewDto>(user);
            }
        }

        public IActionResult OnGetLogin(string returnUrl)
        {
            var url = OAuthApi.GetAuthorizeUrl(appId,
                   "https://707a-122-97-147-54.ngrok.io/MP/OAuth?handler=CallBack&returnUrl=" + returnUrl.UrlEncode(),
                   null, OAuthScope.snsapi_userinfo);//snsapi_userinfo方式回调地址

            return Redirect(url);
        }

        public async Task<IActionResult> OnGetCallBackAsync(string code, string returnUrl)
        {
            if (code.IsNullOrEmpty())
            {
                return Content("您拒绝了授权！");
            }

            var oauthAccessTokenResult = await OAuthApi.GetAccessTokenAsync(appId, appSecret, code);

            var openId = oauthAccessTokenResult.openid;
            var accessToken = oauthAccessTokenResult.access_token;

            var userInfo = await OAuthApi.GetUserInfoAsync(accessToken, openId);

            var userInfoDto = await _userService.CreateOrUpdateFromOAuthAsync(userInfo.openid, userInfo.nickname, userInfo.headimgurl, userInfo.unionid);

            //TODO：记录用户登录信息
            _httpContextAccessor.HttpContext.Session.SetInt32("UserId", userInfoDto.Id);

            if (returnUrl.IsNullOrEmpty())
            {
                return Redirect("/MP/OAuth");
            }

            return Redirect(returnUrl);
        }

        public IActionResult OnGetLogout()
        {
            _httpContextAccessor.HttpContext.Session.Remove("UserId");
            return Redirect("/MP/OAuth");
        }
    }

    internal class UserInfoDto
    {
    }
}
