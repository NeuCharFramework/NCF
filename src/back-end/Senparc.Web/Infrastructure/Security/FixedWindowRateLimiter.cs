/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FixedWindowRateLimiter.cs
    文件功能描述：固定窗口（按客户端）限流器，用于 Cloudflare 及类似防护服务

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.39.0 新增宿主安全防护与 MySQL 数据库集成

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    /// 线程安全的固定窗口限流器。窗口按时间戳对齐（便于多实例在各自进程内独立计数）。
    /// </summary>
    public sealed class FixedWindowRateLimiter
    {
        private readonly ConcurrentDictionary<string, (long Window, long Count)> _store = new();

        /// <summary>
        /// 记录一次请求并判断该客户端在当前窗口内是否已超限。
        /// </summary>
        /// <param name="key">客户端标识（如 IP）。</param>
        /// <param name="now">当前时间（可注入，便于测试）。</param>
        /// <param name="maxRequests">窗口内允许的最大请求数；&lt;=0 表示不限流。</param>
        /// <param name="window">窗口大小。</param>
        /// <returns>是否已超出限制（返回 true 表示应拒绝该请求）。</returns>
        public bool IsLimited(string key, DateTimeOffset now, int maxRequests, TimeSpan window)
        {
            if (string.IsNullOrEmpty(key) || maxRequests <= 0 || window <= TimeSpan.Zero)
            {
                return false;
            }

            var windowMs = Math.Max(1, (long)window.TotalMilliseconds);
            var windowIndex = now.ToUnixTimeMilliseconds() / windowMs;

            long count = 0;
            _store.AddOrUpdate(
                key,
                _ =>
                {
                    count = 1;
                    return (windowIndex, 1L);
                },
                (_, existing) =>
                {
                    if (existing.Window == windowIndex)
                    {
                        count = existing.Count + 1;
                        return (existing.Window, existing.Count + 1);
                    }

                    count = 1;
                    return (windowIndex, 1L);
                });

            return count > maxRequests;
        }

        /// <summary>
        /// 清理早于当前窗口的键，防止内存无限增长。
        /// </summary>
        /// <returns>被清理的键数量。</returns>
        public int Prune(DateTimeOffset now, TimeSpan window)
        {
            if (window <= TimeSpan.Zero)
            {
                return 0;
            }

            var windowMs = Math.Max(1, (long)window.TotalMilliseconds);
            var currentIndex = now.ToUnixTimeMilliseconds() / windowMs;
            var removed = 0;
            foreach (var kv in _store)
            {
                if (kv.Value.Window < currentIndex && _store.TryRemove(kv.Key, out _))
                {
                    removed++;
                }
            }

            return removed;
        }
    }
}
