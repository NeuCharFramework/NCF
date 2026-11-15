/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CloudflareProtectMiddlewareExtensions.cs
    文件功能描述：Cloudflare 及类似站点防护中间件的注册扩展

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.39.0 新增宿主安全防护与 MySQL 数据库集成

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;

namespace Senparc.Web.Infrastructure.Security
{
    public static class CloudflareProtectMiddlewareExtensions
    {
        /// <summary>
        /// 启用 Cloudflare 及类似站点防护中间件。
        /// <para>是否实际生效由 <see cref="CloudflareProtectOptions.Enabled"/> 决定；启用后自站点打开起立即生效。</para>
        /// </summary>
        public static IApplicationBuilder UseCloudflareProtect(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CloudflareProtectMiddleware>();
        }
    }
}
