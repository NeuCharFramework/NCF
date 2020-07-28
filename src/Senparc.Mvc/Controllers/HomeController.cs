using Senparc.Mvc.Filter;
using Microsoft.AspNetCore.Mvc;
using Senparc.Mvc.Models.VD;

namespace Senparc.Mvc.Controllers
{
    [MenuFilter("Home")]
    public class HomeController : BaseFrontController
    {
       
        public HomeController()
        {
         
        }

        public ActionResult Index()
        {
            var vd = new Home_IndexVD()
            {
            };
         
            return View(vd);
        }

        public ActionResult Detail(int id)
        {
            var vd = new Home_DetailVD()
            {
            };
            return View(vd);
        }
        public ActionResult List()
        {
            return View();
        }
    }
}
