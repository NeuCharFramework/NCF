using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Senparc.Xncf.Swagger.Utils
{
    public class VersionHelper
    {
        /// <summary>
        /// 扫描整个应用程序获取实现ApiVersion的方法，从而获取使用过的版本
        /// 效率较低，ApiVersion是否有提供所有使用过的版本接口？？？
        /// </summary>
        /// <returns></returns>
        public static List<string> GetApiVersions()
        {
            var versions = new List<ApiVersion>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                //类上的ApiVersion
                var allTypes = assembly.GetTypes();
                foreach (var t in allTypes)
                {
                    var classVersion = t.GetCustomAttributes(typeof(ApiVersionAttribute), true).OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions).Distinct();
                    versions.AddRange(classVersion);

                    var methods = t.GetMethods().Where(w => w.GetCustomAttributes(typeof(ApiVersionAttribute), true).Length > 0);
                    foreach (var method in methods)
                    {
                        var methodVersion = method.GetCustomAttributes(typeof(ApiVersionAttribute), true).OfType<ApiVersionAttribute>()
                            .SelectMany(attr => attr.Versions).Distinct();
                        versions.AddRange(methodVersion);
                    }
                }
                ////方法上的apiversion
                //var subTypes = allTypes.Where(w => w.GetCustomAttributes(typeof(ApiVersionAttribute), true).Length > 0);
                //foreach (var t in subTypes)
                //{
                //    var methods = t.GetMethods().Where(w => w.GetCustomAttributes(typeof(ApiVersionAttribute), true).Length > 0);
                //    foreach (var method in methods)
                //    {
                //        var methodVersion = method.GetCustomAttributes(typeof(ApiVersionAttribute), true).OfType<ApiVersionAttribute>()
                //            .SelectMany(attr => attr.Versions).Distinct();
                //        versions.AddRange(methodVersion);
                //    }
                //}
            }
            return versions.Distinct().Select(s => $"v{s.MajorVersion}.{s.MinorVersion}".Trim('.')).OrderBy(o => o).ToList();
        }
    }
}
