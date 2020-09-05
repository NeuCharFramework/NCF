using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Senparc.Xncf.Swagger.Filters;
using Senparc.Xncf.Swagger.Models;
using Senparc.Xncf.Swagger.Utils;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace Senparc.Xncf.Swagger.Builder
{
    public static class CustomSwaggerServiceCollectionExtensions
    {
        public static IServiceCollection AddSwaggerCustom(this IServiceCollection services)
        {
            var options = ConfigurationHelper.CustsomSwaggerOptions;
            services.AddSwaggerGen(c =>
            {
                if (options.ApiVersions == null) return;
                foreach (var version in options.ApiVersions)
                {
                    c.SwaggerDoc(version, new OpenApiInfo { Title = options.ProjectName, Version = version });
                }
                //分组
                c.TagActionsBy(s =>
                {
                    var controller = s.ActionDescriptor.RouteValues["controller"];
                    if (s.ActionDescriptor.RouteValues.ContainsKey("area"))
                    {
                        var area = s.ActionDescriptor.RouteValues["area"];
                        if (area != null)
                            return new string[] { $"{area}_{controller}" };
                    }
                    return new string[] { controller };
                });
                //版本
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;

                    var methodVersions = methodInfo
                        .GetCustomAttributes(true)
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);
                    var allow = methodVersions.Any(v => $"v{v.MajorVersion}.{v.MinorVersion}".Trim('.') == docName);
                    if (allow) return true;

                    var declaringTypeVersions = methodInfo.DeclaringType
                        .GetCustomAttributes(true)
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);

                    return declaringTypeVersions.Any(v => $"v{v.MajorVersion}.{v.MinorVersion}".Trim('.') == docName);
                });
                //c.OperationFilter<SwaggerDefaultValueFilter>();
                c.OperationFilter<SwaggerFileUploadFilter>();
                options.AddSwaggerGenAction?.Invoke(c);
            });
            return services;
        }
    }
}
