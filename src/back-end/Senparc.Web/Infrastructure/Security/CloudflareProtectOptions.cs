/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CloudflareProtectOptions.cs
    文件功能描述：Cloudflare 及类似站点防护服务的系统配置

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.39.0 新增宿主安全防护与 MySQL 数据库集成

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    /// Cloudflare 及类似站点防护服务的系统配置。
    /// <para>当 <see cref="Enabled"/> 为 true 时，站点一打开（首个请求起）即立即生效。</para>
    /// </summary>
    public sealed class CloudflareProtectOptions
    {
        /// <summary>
        /// 配置节名称（appsettings.json）。
        /// </summary>
        public const string SectionName = "CloudflareProtect";

        /// <summary>
        /// 是否启用防护（默认 false，禁用）。
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 限流窗口（秒）。
        /// </summary>
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// 窗口内每个客户端允许的最大请求数。
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } = 120;

        /// <summary>
        /// 是否添加安全响应头。
        /// </summary>
        public bool AddSecurityHeaders { get; set; } = true;

        /// <summary>
        /// 受保护的路径前缀；为空表示保护全部路径。
        /// </summary>
        public List<string> ProtectedPathPrefixes { get; set; } = new List<string>();
    }
}
