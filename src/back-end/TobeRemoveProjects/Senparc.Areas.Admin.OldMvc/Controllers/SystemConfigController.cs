using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Models.VD;
using Senparc.Mvc.Filter;

namespace Senparc.Areas.Admin.Controllers
{
    [MenuFilter("SystemConfig")]
    public class SystemConfigController : BaseAdminController
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(SystemConfig_IndexVD model)
        {
            return RedirectToAction("Index");
        }
    }
}