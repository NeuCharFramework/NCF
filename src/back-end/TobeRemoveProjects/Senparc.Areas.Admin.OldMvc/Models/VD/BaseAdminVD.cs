
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;

namespace Senparc.Areas.Admin.Models.VD
{
    public interface IBaseAdminVD : IBaseUiVD
    {
        RouteData RouteData { get; set; }
    }

    public class BaseAdminVD : IBaseAdminVD
    {
        public bool IsAdmin { get; set; }
        public MetaCollection MetaCollection { get; set; }
        public string UserName { get; set; }
        public RouteData RouteData { get; set; }
        public string CurrentMenu { get; set; }
        public List<Messager> MessagerList { get; set; }
        public DateTime PageStartTime { get; set; }
        public DateTime PageEndTime { get; set; }
    }
}
