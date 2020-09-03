using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.FileServer
{
    public partial class Register : IAreaRegister //注册 XNCF 页面接口（按需选用）
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/FileServer/Index";

        public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>() {
            new AreaPageMenuItem(GetAreaHomeUrl(),"文件列表","fa fa-file"),
            new AreaPageMenuItem(GetAreaUrl($"/Admin/FileServer/AppManager"),"App管理","fa fa-cubes")
        };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IWebHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //此处可配置页面权限
            });

            SenparcTrace.SendCustomLog("FileServer 启动", "完成 Area:Senparc.Xncf.FileServer 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.FileServer"));
        #endregion
    }
}
