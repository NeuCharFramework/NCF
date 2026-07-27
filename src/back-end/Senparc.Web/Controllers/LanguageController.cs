/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LanguageController.cs
    文件功能描述：站点语言切换控制器
    
    
    创建标识：Senparc - 20260403
    
    修改标识：Senparc - 20260724
    修改描述：v0.34.0 完善站点本地化及内嵌 WebView 导航兼容性

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Senparc.Web.Controllers
{
    /// <summary>
    /// Language/Culture switcher controller.
    /// Sets the culture cookie that ASP.NET Core's CookieRequestCultureProvider reads on every request.
    /// The cookie persists the selection in the browser for 1 year.
    ///
    /// Endpoint: GET /api/Language/Set?culture=en&amp;returnUrl=/Admin
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LanguageController : ControllerBase
    {
        /// <summary>
        /// All cultures supported by the application.
        /// When adding a new language, also add the culture code here and create the
        /// corresponding *.resx files in Resources/ for each module.
        /// </summary>
        public static readonly string[] SupportedCultures =
            new[] { "zh-CN", "en", "ja", "fr", "es", "ru" };

        /// <summary>
        /// Sets the language preference cookie and redirects to the return URL.
        /// </summary>
        /// <param name="culture">Target culture code, e.g. "en", "zh-CN", "ja"</param>
        /// <param name="returnUrl">URL to redirect to after switching. Defaults to "/"</param>
        [HttpGet("Set")]
        public IActionResult Set(string culture, string returnUrl = "/")
        {
            // Validate culture to prevent injection; fall back to default if unknown
            if (string.IsNullOrWhiteSpace(culture) || !Array.Exists(SupportedCultures, c => c == culture))
            {
                culture = SupportedCultures[0]; // zh-CN
            }

            // Set the culture cookie (name matches CookieRequestCultureProvider.DefaultCookieName)
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    MaxAge = TimeSpan.FromDays(365),
                    IsEssential = true,      // GDPR: mark as essential so consent isn't required for functionality
                    SameSite = SameSiteMode.Lax
                }
            );

            // Validate returnUrl to prevent open-redirect attacks
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            return LocalRedirect(returnUrl);
        }
    }
}
