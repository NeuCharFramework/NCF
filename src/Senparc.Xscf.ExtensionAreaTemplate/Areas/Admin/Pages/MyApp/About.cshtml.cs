using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.ExtensionAreaTemplate.Areas.MyApp.Pages
{
    public class About : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public About(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {
        }
    }
}
