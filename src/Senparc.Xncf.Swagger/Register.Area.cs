using Enyim.Caching.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.Swagger.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.Swagger
{
    public partial class Register : IAreaRegister //注册 XNCF 页面接口（按需选用）
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/Swagger/Index";

        public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>() {
             new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
                     };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(Utils.ConfigurationHelper.SWAGGER_ATUH_COOKIE, options =>
                {
                    if (Utils.ConfigurationHelper.CustsomSwaggerOptions.UseAdminAuth)
                        options.LoginPath = "/Admin/Login/";
                    else
                        options.LoginPath = $"/{Utils.ConfigurationHelper.CustsomSwaggerOptions.RoutePrefix}/login.html";
                    options.Cookie.HttpOnly = false;
                });

            builder.AddRazorPagesOptions(options =>
            {
                //此处可配置页面权限
            });

            SenparcTrace.SendCustomLog("Swagger 启动", "完成 Area:Senparc.Xncf.Swagger 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Swagger"));
        #endregion
    }
}
