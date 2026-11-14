/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CloudflareProtectMiddleware.cs
    文件功能描述：Cloudflare 及类似站点防护中间件（限流 + 安全响应头）

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.39.0 新增宿主安全防护与 MySQL 数据库集成

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    /// Cloudflare 及类似站点防护中间件。
    /// <para>
    /// 当 <see cref="CloudflareProtectOptions.Enabled"/> 为 true 时，自站点打开（首个请求）起立即生效：
    /// 为受保护请求添加安全响应头，并按客户端（IP）进行固定窗口限流，超限返回 429。
    /// </para>
    /// </summary>
    public sealed class CloudflareProtectMiddleware
    {
        private readonly RequestDelegate _next;

        // 上次清理时间，避免每个请求都遍历字典。
        private DateTime _lastPruneUtc = DateTime.MinValue;

        public CloudflareProtectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var options = context.RequestServices.GetRequiredService<IOptions<CloudflareProtectOptions>>().Value;
            if (!options.Enabled)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            if (options.AddSecurityHeaders)
            {
                ApplySecurityHeaders(context.Response);
            }

            if (IsPathProtected(context.Request.Path, options))
            {
                var limiter = context.RequestServices.GetRequiredService<FixedWindowRateLimiter>();
                var window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds));
                var now = DateTimeOffset.UtcNow;
                PruneIfNeeded(limiter, window, now);

                var clientKey = GetClientKey(context);
                if (limiter.IsLimited(clientKey, now, options.MaxRequestsPerWindow, window))
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["Retry-After"] = Math.Max(1, options.WindowSeconds).ToString();
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "rate_limited",
                        message = "请求过于频繁，请稍后再试。"
                    }).ConfigureAwait(false);
                    return;
                }
            }

            await _next(context).ConfigureAwait(false);
        }

        private static bool IsPathProtected(PathString path, CloudflareProtectOptions options)
        {
            var prefixes = options.ProtectedPathPrefixes;
            if (prefixes == null || prefixes.Count == 0)
            {
                return true;
            }

            return prefixes.Any(p => !string.IsNullOrWhiteSpace(p) && path.StartsWithSegments(p));
        }

        private static string GetClientKey(HttpContext context)
        {
            var remote = context.Connection.RemoteIpAddress;
            return remote?.MapToIPv4()?.ToString() ?? "unknown";
        }

        private void PruneIfNeeded(FixedWindowRateLimiter limiter, TimeSpan window, DateTimeOffset now)
        {
            var nowUtc = now.UtcDateTime;
            if ((nowUtc - _lastPruneUtc).TotalSeconds < 1)
            {
                return;
            }

            lock (this)
            {
                if ((nowUtc - _lastPruneUtc).TotalSeconds < 1)
                {
                    return;
                }

                _lastPruneUtc = nowUtc;
            }

            limiter.Prune(now, window);
        }

        private static void ApplySecurityHeaders(HttpResponse response)
        {
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            response.Headers["X-XSS-Protection"] = "0";
        }
    }
}
