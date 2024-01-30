using Senparc.Ncf.Service;
using System;

namespace SenparcConf.Xncf.WechatBot.Areas.WechatBot.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public Index(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {

        }

        public void OnGet()
        {
        }
    }
}
