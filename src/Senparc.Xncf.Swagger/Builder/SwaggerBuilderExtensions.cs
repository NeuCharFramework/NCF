using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Weixin.MP.AdvancedAPIs.Semantic;
using Senparc.Xncf.Swagger.Models;
using Senparc.Xncf.Swagger.Utils;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace Senparc.Xncf.Swagger.Builder
{
    public static class SwaggerBuilderExtensions
    {
        public static IApplicationBuilder UseSwaggerCustom(this IApplicationBuilder app)
        {
            var options = ConfigurationHelper.CustsomSwaggerOptions;
            app
            //.UseSwaggerCustomAuth(options)
            .UseSwagger(opt =>
            {
                opt.RouteTemplate = $"/{options.RoutePrefix}/{{documentName}}/swagger.json";
                if (options.UseSwaggerAction == null) return;
                options.UseSwaggerAction(opt);
            })
            .UseSwaggerUI(c =>
            {
                c.RoutePrefix = options.RoutePrefix;
                c.DocumentTitle = options.ProjectName;
                if (options.UseCustomIndex)
                {
                    c.UseCustomSwaggerIndex();
                }
                if (options.CustomAuthList?.Count > 0)
                {
                    //c.ConfigObject["customAuth"] = true;
                    //c.ConfigObject["loginUrl"] = $"/{options.RoutePrefix}/login.html";
                    //c.ConfigObject["logoutUrl"] = $"/{options.RoutePrefix}/logout";
                }
                if (options.ApiVersions == null) options.ApiVersions = new List<string> { "v1" };
                foreach (var item in options.ApiVersions)
                {
                    var subPath = string.IsNullOrEmpty(options.AppPath) ? "" : $"/{options.AppPath}";//发布为虚拟站点时使用
                    c.SwaggerEndpoint($"{subPath}/{options.RoutePrefix}/{item}/swagger.json", $"{item}");
                }
                options.UseSwaggerUIAction?.Invoke(c);
            });
            return app;
        }
        /// <summary>
        /// 处理接口文档的用户认证
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static IApplicationBuilder UseSwaggerCustomAuth(this IApplicationBuilder app, CustsomSwaggerOptions options)
        {
            if (options.AllowAnonymous)
                return app;
            var currentAssembly = typeof(CustsomSwaggerOptions).GetTypeInfo().Assembly;
            app.Use(async (context, next) =>
            {
                var _method = context.Request.Method.ToLower();
                var _path = context.Request.Path.Value;
                var subPath = string.IsNullOrEmpty(options.AppPath) ? "" : $"/{options.AppPath}";//发布为虚拟站点时使用
                #region 自定义登录页
                if (_path.IndexOf($"/{options.RoutePrefix}") != 0)//非访问接口时直接返回
                {
                    await next();
                    return;
                }
                else if (_path == $"/{options.RoutePrefix}/login.html")
                {
                    //登录
                    if (_method == "get")
                    {
                        var stream = currentAssembly.GetManifestResourceStream($"{currentAssembly.GetName().Name}.login.html");
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        context.Response.ContentType = "text/html;charset=utf-8";
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                        return;
                    }
                    else if (_method == "post")
                    {
                        var userModel = new CustomSwaggerAuth(context.Request.Form["userName"], context.Request.Form["userPwd"]);
                        if (!options.CustomAuthList.Any(e => e.UserName == userModel.UserName && e.UserPwd == userModel.UserPwd))
                        {
                            await context.Response.WriteAsync("login error!");
                            return;
                        }
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name,userModel.UserName)
                        };
                        var identity = new ClaimsIdentity(ConfigurationHelper.SWAGGER_ATUH_COOKIE);
                        identity.AddClaims(claims);
                        var authProperties = new AuthenticationProperties
                        {
                            AllowRefresh = false,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120),
                            IsPersistent = false,
                        };
                        await context.SignOutAsync(ConfigurationHelper.SWAGGER_ATUH_COOKIE);//登出
                        await context.SignInAsync(ConfigurationHelper.SWAGGER_ATUH_COOKIE, new ClaimsPrincipal(identity), authProperties);

                        context.Response.Redirect($"{subPath}/{options.RoutePrefix}");
                        return;
                    }
                }
                else if (_path == $"/{options.RoutePrefix}/logout")
                {
                    //退出
                    context.Response.Cookies.Delete(ConfigurationHelper.SWAGGER_ATUH_COOKIE);
                    context.Response.Redirect($"{subPath}/{options.RoutePrefix}/login.html");
                    return;
                }
                #endregion
                else
                {
                    if (ConfigurationHelper.CustsomSwaggerOptions.UseAdminAuth) {
                        var authentcationResult = await context.AuthenticateAsync(AdminAuthorizeAttribute.AuthenticationScheme);
                        if (!authentcationResult.Succeeded)
                        {
                            context.Response.Redirect("/Admin/Login/");
                            return;
                        }
                    }
                    else
                    {
                        var authentcationResult = await context.AuthenticateAsync(ConfigurationHelper.SWAGGER_ATUH_COOKIE);
                        if (!authentcationResult.Succeeded)
                        {
                            context.Response.Redirect($"{subPath}/{options.RoutePrefix}/login.html");
                            return;
                        }
                    }
                }
                await next();
            });
            return app;
        }
        /// <summary>
        /// 使用自定义首页
        /// </summary>
        /// <returns></returns>
        private static void UseCustomSwaggerIndex(this SwaggerUIOptions c)
        {
            var currentAssembly = typeof(CustsomSwaggerOptions).GetTypeInfo().Assembly;
            c.IndexStream = () => currentAssembly.GetManifestResourceStream($"{currentAssembly.GetName().Name}.index.html");
        }
    }
}
