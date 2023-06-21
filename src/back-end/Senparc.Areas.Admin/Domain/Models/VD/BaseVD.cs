using Microsoft.AspNetCore.Routing;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.VD
{
      public interface IBaseVD : IBaseUiVD
    {
        FullSystemConfig FullSystemConfig { get; set; }

        RouteData RouteData { get; set; }

        //FullAccount FullAccount { get; set; }
    }


    public class BaseVD : IBaseVD
    {
        public FullSystemConfig FullSystemConfig { get; set; }

        public MetaCollection MetaCollection { get; set; }

        public string UserName { get; set; }

        public bool IsAdmin { get; set; }

        public RouteData RouteData { get; set; }

        public string CurrentMenu { get; set; }

        public List<Messager> MessagerList { get; set; }

        //public FullAccount FullAccount { get; set; }

        public DateTime PageStartTime { get; set; }

        public DateTime PageEndTime { get; set; }
    }


    public class SuccessVD : BaseVD
    {
        public string Message { get; set; }

        public string BackUrl { get; set; }

        public string BackAction { get; set; }

        public string BackController { get; set; }

        public RouteValueDictionary BackRouteValues { get; set; }

        public bool CountDown { get; set; }

        public string TipMessage { get; set; }
    }
}