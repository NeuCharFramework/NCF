using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.FileServer.Utility
{
    /// <summary>
    /// Configuration帮助类
    /// </summary>
    public class ConfigurationHelpler
    {
        /// <summary>
        /// 全局配置
        /// </summary>
        public static IConfiguration Configuration;

        /// <summary>
        /// 模块自定义配置文件
        /// </summary>
        public static IConfiguration CustomConfiguration;

        /// <summary>
        /// 托管环境信息
        /// </summary>
        public static IHostEnvironment HostEnvironment;

        /// <summary>
        /// Web托管环境信息
        /// </summary>
        public static IWebHostEnvironment WebHostEnvironment;

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
