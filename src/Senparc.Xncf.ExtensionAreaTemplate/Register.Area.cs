using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.ExtensionAreaTemplate
{
	public partial class Register : IAreaRegister //注册 XNCF 页面接口（按需选用）
	{
		#region IAreaRegister 接口

		public string HomeUrl => "/Admin/MyApp/MyHomePage";

		public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>() {
			 new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
			 new AreaPageMenuItem(GetAreaUrl("/Admin/MyApp/About"),"关于","fa fa-bookmark-o"),
		};

		public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IWebHostEnvironment env)
		{
			builder.AddRazorPagesOptions(options =>
			{
				//此处可配置页面权限
			});

			SenparcTrace.SendCustomLog("系统启动", "完成 Area:MyApp 注册");

			return builder;
		}

		#endregion

		#region IXncfRazorRuntimeCompilation 接口
		public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.ExtensionAreaTemplate"));
		#endregion
	}
}
