using Senparc.Ncf.Service;
using Senparc.Xncf.Swagger.Utils;
using System;

namespace Senparc.Xncf.Swagger.Areas.Swagger.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public string RoutePrefix { get; set; }

        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            RoutePrefix = ConfigurationHelper.CustsomSwaggerOptions.RoutePrefix;
        }

        public void OnGet()
        {
        }
    }
}
