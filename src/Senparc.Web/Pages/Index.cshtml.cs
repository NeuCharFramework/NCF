using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models;
using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;

namespace Senparc.Web.Pages
{
    public class IndexModel : BasePageModel
    {
        IServiceProvider _serviceProvider;

        public IndexModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string Output { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            //Process p = new Process();
            //p.StartInfo.FileName = "cmd.exe";
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardInput = true;
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.RedirectStandardError = true;
            //p.StartInfo.CreateNoWindow = true;
            //p.StandardInput.WriteLine("dotnet --version");
            //Output = p.StandardOutput.ReadToEnd();

            //var wsh = new PowerShellHelper();
            //Output = wsh.Execute("dotnet --version");

            var senparcEntities = _serviceProvider.GetService<SenparcEntities>();


            var pendingMigrations = senparcEntities.Database.GetPendingMigrations();
            var oldMigrations = senparcEntities.Database.GetAppliedMigrations();

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
