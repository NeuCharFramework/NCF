using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.FileServer.Services;
using System;
using System.IO;

namespace Senparc.Xncf.FileServer
{
    public partial class Register : IXncfMiddleware
    {
        public IApplicationBuilder UseMiddleware(IApplicationBuilder app)
        {
            var staticResourceSetting = app.ApplicationServices.GetService<IOptionsMonitor<StaticResourceSetting>>();
            //静态资源允许跨域
            var path = Path.Combine(Directory.GetCurrentDirectory(), staticResourceSetting.CurrentValue.RootDir);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var fileOptions = new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(path),
                RequestPath = staticResourceSetting.CurrentValue.RequestPath,
                OnPrepareResponse = (x) =>//验证静态资源授权
                {
                    var token = x.Context.Request.Query["token"];
                    if (string.IsNullOrWhiteSpace(token))
                        token = x.Context.Request.Headers["token"];
                    try
                    {
                        x.Context.RequestServices.GetService<SysKeyService>().ValidToken(token).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        x.Context.Response.StatusCode = StatusCodes.Status404NotFound;
                        x.Context.Response.WriteAsync(ex.Message);
                    }
                    //new StatusCodes().Status401Unauthorized
                    //x.Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");//允许跨域，Core已做处理
                }
            };
            app.UseStaticFiles(fileOptions);
            app.UseCors();

            //添加MVC模式支持
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
            return app;
        }
    }
}
