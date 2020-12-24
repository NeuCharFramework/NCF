using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XY.Xncf.SignalR.Middlewares
{
    public class SignalRClientMiddleware
    {
        private readonly RequestDelegate _next;
        public SignalRClientMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var Request = httpContext.Request;
            var signalrClient = File.ReadAllText($"{Senparc.CO2NET.Utilities.ServerUtility.AppDomainAppPath}/Shared/SignalR.cshtml");
            return _next(httpContext);
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
