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

namespace Senparc.Xncf.MaQueKeTang
{
    public partial class Register : IAreaRegister, //注册 XNCF 页面接口（按需选用）
                                    IXncfRazorRuntimeCompilation  //赋能 RazorPage 运行时编译
    {
        #region IAreaRegister 接口
        public string HomeUrl => "/Admin/MaQueKeTang/Index";

        public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>() {
             new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
             new AreaPageMenuItem(GetAreaUrl($"/Admin/CourseCategory/Index"),"课程分类","fa fa-bookmark-o"),
             new AreaPageMenuItem(GetAreaUrl($"/Admin/Course/Index"),"课程","fa fa-bookmark-o"),
             new AreaPageMenuItem(GetAreaUrl($"/Admin/Video/Index"),"视频","fa fa-laptop"),
             new AreaPageMenuItem(GetAreaUrl($"/Admin/CourseComment/Index"),"课程评价","fa fa-laptop"),
             new AreaPageMenuItem(GetAreaUrl($"/Admin/VideoUserComment/Index"),"视频评论","fa fa-laptop"),
        };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //此处可配置页面权限
            });

            SenparcTrace.SendCustomLog("MaQueKeTang 启动", "完成 Area:Senparc.Xncf.MaQueKeTang 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.MaQueKeTang"));
        #endregion
    }
}
