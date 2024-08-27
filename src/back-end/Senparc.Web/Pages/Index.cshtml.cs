using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
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

        public IndexModel(IServiceProvider serviceProvider, RequestTenantInfo requestTenantInfo)
        {
            _serviceProvider = serviceProvider;
            RequestTenantInfo = requestTenantInfo;
        }

        public async Task<IActionResult> OnGetAsync(string forceUpdateModule)
        {


            //判断是否需要自动进入到安装程序
            if (base.FullSystemConfig == null)
            {

                return new RedirectResult("/Install");
            }
            return Page();
        }

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            if (Request.IsLocal())
            {
                var forceUpdateModule = context.HttpContext.Request.Query["forceUpdateModule"].ToString();
                if (!forceUpdateModule.IsNullOrEmpty())
                {
                    //强制本地安装
                    SenparcTrace.SendCustomLog("强制更新模块", $"开始：{forceUpdateModule}");
                    var register = new Senparc.Xncf.SystemManager.Register();
                    await register.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Update);
                    SenparcTrace.SendCustomLog("强制更新模块", $"完成：{forceUpdateModule}");
                }

                await base.OnPageHandlerExecutionAsync(context, next);
            }
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
