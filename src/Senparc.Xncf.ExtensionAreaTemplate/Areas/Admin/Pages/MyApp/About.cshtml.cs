using Senparc.Ncf.Service;
using System;

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
