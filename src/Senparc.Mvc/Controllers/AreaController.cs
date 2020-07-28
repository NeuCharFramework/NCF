using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Area;

namespace Senparc.Mvc.Controllers
{
    public class AreaController : BaseHttpsRequireController//NOT BaseController
    {
        [HttpPost]
        public ActionResult GetAreaJsonData(string area, string name)
        {
            var areaData = new AreaData();

            switch (area)
            {
                case "province":
                    var cityDataList = areaData.GetCitiesData(name);
                    return Json(cityDataList);
                case "city":
                    var districtDataList = areaData.GetDistrictsData(name);
                    return Json(districtDataList);
            }
            return null;
        }
    }
}
