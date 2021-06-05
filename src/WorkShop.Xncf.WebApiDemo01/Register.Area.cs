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
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace WorkShop.Xncf.WebApiDemo01
{
	public partial class Register : IAreaRegister, //注册 XNCF 页面接口（按需选用）
									IXncfRazorRuntimeCompilation  //赋能 RazorPage 运行时编译
	{
		#region IAreaRegister 接口

		public string HomeUrl => "/Admin/Admin/Index";

		public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>() {
			 new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
        new AreaPageMenuItem(GetAreaUrl($"/Admin/WebApiDemo01/DatabaseSample"),"颜色管理","fa fa-bookmark-o"),
			 		};

		public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
		{
			builder.AddRazorPagesOptions(options =>
			{
				//此处可配置页面权限
			});

			SenparcTrace.SendCustomLog("Admin 启动", "完成 Area:AllTheCode.Xncf.Admin 注册");

			return builder;
		}

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });

            return base.UseXncfModule(app, registerService);
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "WorkShop.Xncf.WebApiDemo01"));
		#endregion
	}
}
