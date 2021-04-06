using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using System;
using System.Collections.Generic;
using System.Text;

namespace XY.Xncf.SignalR
{
    public partial class Register : IAreaRegister
    {
        public string HomeUrl => "/Admin/SignalR/Index";

        public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>() {
             new AreaPageMenuItem(GetAreaHomeUrl(),"在线用户","fa fa-user"),
                     };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            return builder;
        }
    }
}
