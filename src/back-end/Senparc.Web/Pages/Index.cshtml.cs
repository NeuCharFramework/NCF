/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml.cs 相关实现


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20260729
    修改描述：v0.34.1 完善站点初始化状态、浏览器导航与多语言提示

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Web.Models.VD;
using System;
//using System.Management.Automation;
using System.Threading.Tasks;

namespace Senparc.Web.Pages
{
    public class IndexModel : BasePageModel
    {
        IServiceProvider _serviceProvider;

        public string Output { get; set; }
        public RequestTenantInfo RequestTenantInfo { get; }
        private readonly IStringLocalizer<SharedResource> _localizer;

        public IndexModel(IServiceProvider serviceProvider, RequestTenantInfo requestTenantInfo,
            IStringLocalizer<SharedResource> localizer)
        {
            _serviceProvider = serviceProvider;
            RequestTenantInfo = requestTenantInfo;
            _localizer = localizer;
        }

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context,
            PageHandlerExecutionDelegate next)
        {
            await base.OnPageHandlerExecutionAsync(context, next);

            // PageModelBase performs the database/installation-state check
            // before OnGetAsync. Replace only this page's automatic redirect
            // with a localized, browser-friendly transition page.
            if (SiteConfig.IsInstalling
                && context.Result is RedirectResult redirect
                && string.Equals(redirect.Url, "/Install", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = BuildInstallRedirectPage();
            }
        }

        public Task<IActionResult> OnGetAsync(string forceUpdateModule)
        {
            //判断是否需要自动进入到安装程序
            if (base.FullSystemConfig == null)
            {
                return Task.FromResult<IActionResult>(BuildInstallRedirectPage());
            }
            return Task.FromResult<IActionResult>(Page());
        }

        private ContentResult BuildInstallRedirectPage()
        {
            var title = _localizer["Home.InstallRequired.Title"].Value;
            var message = _localizer["Home.InstallRequired.Message"].Value;
            var link = _localizer["Home.InstallRequired.Link"].Value;

            var html = $$"""
                <!DOCTYPE html>
                <html lang="{{System.Globalization.CultureInfo.CurrentUICulture.Name}}">
                <head>
                    <meta charset="utf-8" />
                    <meta http-equiv="refresh" content="1.5;url=/Install" />
                    <meta name="viewport" content="width=device-width, initial-scale=1" />
                    <title>{{title}}</title>
                    <style>
                        body { margin: 0; min-height: 100vh; display: grid; place-items: center; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; color: #243447; background: #f5f7fa; }
                        main { max-width: 42rem; margin: 1.5rem; padding: 2.5rem 3rem; text-align: center; background: #fff; border-radius: .75rem; box-shadow: 0 .5rem 2rem rgba(36, 52, 71, .12); }
                        p { line-height: 1.8; }
                        a { color: #1677ff; }
                    </style>
                </head>
                <body>
                    <main role="status" aria-live="polite">
                        <h1>{{title}}</h1>
                        <p>{{message}}</p>
                        <p><a href="/Install">{{link}}</a></p>
                    </main>
                    <script>window.setTimeout(function () { window.location.replace('/Install'); }, 1500);</script>
                </body>
                </html>
                """;

            return new ContentResult
            {
                Content = html,
                ContentType = "text/html; charset=utf-8"
            };
        }
    }

    //public class PowerShellHelper
    //{
    //    public string Execute(string command)
    //    {
    //        StringBuilder sb = new StringBuilder();
    //        using (var ps = PowerShell.Create())
    //        {
    //            var results = ps.AddScript(command).Invoke();
    //            foreach (var result in results)
    //            {
    //                Debug.Write(result.ToString());
    //                sb.AppendLine(result.ToString());
    //            }
    //        }
    //        return sb.ToString();
    //    }
    //}
}
