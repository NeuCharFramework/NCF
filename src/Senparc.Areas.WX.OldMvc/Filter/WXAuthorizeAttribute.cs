using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Config;
using Senparc.Weixin;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Senparc.Areas.WX.Filter
{
    /// <summary>
    /// 服务号OAuth2.0的验证
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
            Justification = "Unsealed so that subclassed types can set properties in the default constructor or override our behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class WxAuthorizeAttribute : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var openId = context.HttpContext.Session.GetString("OpenId");
            if (!Config.SenparcWeixinSetting.IsDebug && openId.IsNullOrEmpty())
            {
                var returnUrl = $"{SiteConfig.DomainName}/WX/WeixinOAuth/MpCallback?callbackUrl={(SiteConfig.DomainName + context.HttpContext.Request.Path + context.HttpContext.Request.Query).UrlEncode()}";
                var getCodeUrl = OAuthApi.GetAuthorizeUrl(Config.SenparcWeixinSetting.WeixinAppId, returnUrl, Config.SenparcWeixinSetting.Token, OAuthScope.snsapi_userinfo);

                context.Result = new RedirectResult(getCodeUrl);
            }
        }
    }
}