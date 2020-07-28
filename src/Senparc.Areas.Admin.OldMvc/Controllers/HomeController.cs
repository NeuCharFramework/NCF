using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Models.VD;
using Senparc.Mvc.Filter;

namespace Senparc.Areas.Admin.Controllers
{
    [MenuFilter("Home")]
    public class HomeController : BaseAdminController
    {
        public ActionResult Index()
        {
            var vd = new Home_IndexVD()
            {

            };
            return View(vd);
        }
    }
}
