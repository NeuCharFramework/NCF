using Senparc.Ncf.Service;
using SenparcLive.Xncf.MyApp.Models.DatabaseModel;
using System;

namespace SenparcLive.Xncf.MyApp.Areas.MyApp.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {

        //TODO：需要使用dto!!!!

        public int ViewCount { get; set; }

        public ServiceBase<Counter> _counterService { get; set; }

        public Index(Lazy<XncfModuleService> xncfModuleService, ServiceBase<Counter> counterService) : base(xncfModuleService)
        {
            _counterService = counterService;
        }

        public void OnGet()
        {
            var counter = _counterService.GetObject(z => true);
            if (counter == null)
            {
                counter = new Counter(0);
                _counterService.SaveObject(counter);
            }

            ViewCount = counter.AddView();
            _counterService.SaveObject(counter);
        }
    }
}
