using Senparc.Xncf.Swagger.Utils;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.Swagger.Models
{
    public class CustsomSwaggerOptions
    {
        public CustsomSwaggerOptions() { }
        public CustsomSwaggerOptions(string projectName, List<string> apiVersions)
        {
            ProjectName = projectName;
            ApiVersions = apiVersions;
        }

        /// <summary>
        /// 项目发布路径 子应用程序的目录名（虚拟站点）
        /// 直接发布为站点时，请保持默认值
        /// </summary>
        public string AppPath { get; set; }
        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = "My API";
        /// <summary>
        /// 接口文档显示版本
        /// </summary>
        public List<string> ApiVersions { get; set; } = VersionHelper.GetApiVersions();
        /// <summary>
        /// 接口文档访问路由前缀
        /// </summary>
        public string RoutePrefix { get; set; } = "swagger";
        /// <summary>
        /// 使用自定义首页
        /// </summary>
        public bool UseCustomIndex { get; set; }
        /// <summary>
        /// 允许匿名访问
        /// </summary>
        public bool AllowAnonymous { get; set; }
        /// <summary>
        /// 使用主项目的Admin模块认证登录
        /// </summary>
        public bool UseAdminAuth { get; set; }
        /// <summary>
        /// swagger login账号,未指定则不启用
        /// </summary>
        public List<CustomSwaggerAuth> CustomAuthList { get; set; }
        /// <summary>
        /// UseSwagger Hook
        /// </summary>
        public Action<SwaggerOptions> UseSwaggerAction { get; set; }
        /// <summary>
        /// UseSwaggerUI Hook
        /// </summary>
        public Action<SwaggerUIOptions> UseSwaggerUIAction { get; set; }
        /// <summary>
        /// AddSwaggerGen Hook
        /// </summary>
        public Action<SwaggerGenOptions> AddSwaggerGenAction { get; set; }
    }
}
