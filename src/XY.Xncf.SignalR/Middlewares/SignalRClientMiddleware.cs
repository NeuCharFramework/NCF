using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XY.Xncf.SignalR.Middlewares
{
    public class SignalRClientMiddleware
    {
        private readonly RequestDelegate _next;
        private IHostEnvironment _env;
        public SignalRClientMiddleware(RequestDelegate next, IHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            //参考如下链接修改返回流
            //https://github.com/dotnet/aspnetcore/blob/v3.1.7/src/Middleware/ResponseCompression/src/ResponseCompressionMiddleware.cs

            if (httpContext.WebSockets.IsWebSocketRequest)
                await _next(httpContext);
            else
            {
                // 保持原来的流
                var originalBody = httpContext.Response.Body;

                // 用ms替换当前的流
                var ms = new MemoryStream();
                httpContext.Response.Body = ms;

                await _next(httpContext);

                // 替换其中的内容
                ms.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(ms);
                var str = reader.ReadToEnd();
                var appendStr = "";
                if (str.Contains("<!DOCTYPE"))
                {
                    appendStr = Template.SignalRClientTemplate;
                }
                var doubleStr = str + appendStr;
                var buffer = Encoding.UTF8.GetBytes(doubleStr);

                var ms2 = new MemoryStream();
                ms2.Write(buffer, 0, buffer.Length);
                ms2.Seek(0, SeekOrigin.Begin);

                // 写入到原有的流中
                await ms2.CopyToAsync(originalBody);
            }
        }
    }

    public static class BasicMiddlewareExtensions
    {
        public static IApplicationBuilder UseSignalRClientMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SignalRClientMiddleware>();
        }
    }
}
