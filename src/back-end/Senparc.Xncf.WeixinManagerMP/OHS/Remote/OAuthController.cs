using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerMP.OHS.Remote
{
    public class OAuthController : Controller
    {
        public OAuthController()
        {

        }

        public IActionResult Index()
        {
            return Content("OK");
        }
    }
}
