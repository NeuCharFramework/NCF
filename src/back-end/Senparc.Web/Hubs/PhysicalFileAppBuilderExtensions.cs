using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Web.Hubs
{
    public static class PhysicalFileAppBuilderExtensions
    {
        private static readonly PhysicalFileProvider _fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());

        /// <summary>
        /// 检测文件变化
        /// </summary>
        /// <param name="app"></param>
        /// <param name="hubContext"></param>
        /// <returns></returns>
        public static IApplicationBuilder UsePhysicalFile(this IApplicationBuilder app, IHubContext<ReloadPageHub> hubContext)
        {
            RegisterPhysical(hubContext);
            return app;
        }

        /// <summary>
        /// 注册检查
        /// </summary>
        /// <param name="hubContext"></param>
        public static void RegisterPhysical(IHubContext<ReloadPageHub> hubContext)
        {
            Task.Run(() =>
            {
                var tcs = new TaskCompletionSource<object>();
                while (true)
                {
                    PhysicalFileAsync(hubContext).GetAwaiter().GetResult();
                }
            });
        }

        /// <summary>
        /// 检查文件变化
        /// </summary>
        /// <param name="hubContext"></param>
        /// <returns></returns>
        private static async Task PhysicalFileAsync(IHubContext<ReloadPageHub> hubContext)
        {
            var jsToken = _fileProvider.Watch("wwwroot/**/*.js");
            var cssToken = _fileProvider.Watch("wwwroot/**/*.css");
            var cshtmlToken = _fileProvider.Watch("**/*.cshtml");
            var tcs = new TaskCompletionSource<object>();
            //TODO:如果考虑效率问题可以不使用while(true)，可以选中在RegisterChangeCallback 内部再次调用fileProvider.Watch
            jsToken.RegisterChangeCallback(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                hubContext.Clients.All.SendAsync("ReloadPage", "js文件发生变化");
            }, tcs);
            cssToken.RegisterChangeCallback(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                hubContext.Clients.All.SendAsync("ReloadPage", "css文件发生变化");
            }, tcs);
            cshtmlToken.RegisterChangeCallback(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                hubContext.Clients.All.SendAsync("ReloadPage", "cshtml文件发生变化");
            }, tcs);
            await tcs.Task.ConfigureAwait(false);
        }
    }
}
