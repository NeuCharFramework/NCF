using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> OnGetAsync()
        {
            //判断是否需要自动进入到安装程序
            if (base.FullSystemConfig == null)
            {
                return new RedirectResult("/Install");
            }
            return Page();
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
