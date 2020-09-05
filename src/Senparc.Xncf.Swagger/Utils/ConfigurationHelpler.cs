using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.Swagger.Models;
using System;

namespace Senparc.Xncf.Swagger.Utils
{
    /// <summary>
    /// Configuration帮助类
    /// </summary>
    public class ConfigurationHelper
    {
        /// <summary>
        /// 全局配置
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Swagger模块的配置
        /// </summary>
        public static IConfiguration SwaggerConfiguration { get; set; }

        /// <summary>
        /// Swagger模块的自定义配置信息
        /// </summary>
        public static CustsomSwaggerOptions CustsomSwaggerOptions { get; set; }

        /// <summary>
        /// 托管环境信息
        /// </summary>
        public static IHostEnvironment HostEnvironment { get; set; }

        /// <summary>
        /// Web托管环境信息
        /// </summary>
        public static IWebHostEnvironment WebHostEnvironment { get; set; }

        /// <summary>
        /// 内部访问（项目根路径）
        /// </summary>
        public static string ContentRootPath
        {
            get
            {
                return HostEnvironment.ContentRootPath;
            }
        }

        /// <summary>
        /// web外部访问（wwwroot）
        /// </summary>
        public static string WebRootPath
        {
            get
            {
                if (!string.IsNullOrEmpty(WebHostEnvironment.WebRootPath))
                    return WebHostEnvironment.WebRootPath;
                else
                    return System.IO.Path.Combine(ContentRootPath, "wwwroot");
            }
        }

        /// <summary>
        /// Cookie认证名称
        /// </summary>
        public static readonly string SWAGGER_ATUH_COOKIE = nameof(SWAGGER_ATUH_COOKIE);

        /// <summary>
        /// 获取AppsettingsJson的值
        /// </summary>
        /// <param name="key">键路径，如：ConnectionStrings:SQLServerConn</param>
        /// <returns></returns>
        public static string GetValue(string key)
        {
            return Configuration.GetValue<string>(key);
        }

        /// <summary>
        /// 获取AppsettingsJson的值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="key">键路径</param>
        /// <returns></returns>
        public static T GetValue<T>(string key)
        {
            return Configuration.GetValue<T>(key);
        }
    }
}
