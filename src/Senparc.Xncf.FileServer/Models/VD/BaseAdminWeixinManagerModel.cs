using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.FileServer.Models.VD
{
    public class BaseAdminWeixinManagerModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public BaseAdminWeixinManagerModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }
    }
}
