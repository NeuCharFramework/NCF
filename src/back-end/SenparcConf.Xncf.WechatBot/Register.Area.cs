using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace SenparcConf.Xncf.WechatBot
{
    public partial class Register : IAreaRegister, //注册 XNCF 页面接口（按需选用）
                                    IXncfRazorRuntimeCompilation  //赋能 RazorPage 运行时编译
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/WechatBot/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
			 			 new AreaPageMenuItem(GetAreaUrl($"/Admin/WechatBot/DatabaseSample"),"数据库操作示例","fa fa-bookmark-o")
			 		};

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //此处可配置页面权限
            });

            SenparcTrace.SendCustomLog("WechatBot 启动", "完成 Area:SenparcConf.Xncf.WechatBot 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "SenparcConf.Xncf.WechatBot"));
        #endregion
    }
}
