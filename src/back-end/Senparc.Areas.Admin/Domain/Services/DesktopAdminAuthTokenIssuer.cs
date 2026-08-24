/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopAdminAuthTokenIssuer.cs
    文件功能描述：把已验证的 WebView 管理员身份换成受源会话寿命约束的 JWT

    创建标识：Senparc - 20260804

    修改标识：Senparc - 20260808
    修改描述：v0.4.0 实现 WebView 管理员身份到短期 JWT 的换票签发

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Shared.Abstractions.Security;

namespace Senparc.Areas.Admin.Domain;

public sealed class DesktopAdminAuthTokenIssuer : IDesktopAdminAuthTokenIssuer
{
    private readonly AdminUserInfoService _adminUserInfoService;

    public DesktopAdminAuthTokenIssuer(AdminUserInfoService adminUserInfoService)
    {
        _adminUserInfoService = adminUserInfoService;
    }

    public async Task<DesktopAdminAuthTokenIssueResult> IssueAsync(
        int adminUserId,
        DateTimeOffset sourceAuthenticationExpiresUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (adminUserId <= 0 || sourceAuthenticationExpiresUtc <= DateTimeOffset.UtcNow.AddSeconds(10))
        {
            return new DesktopAdminAuthTokenIssueResult(
                false,
                ErrorMessage: "WebView 管理员登录已过期，请重新登录。");
        }

        var adminUser = await _adminUserInfoService
            .GetObjectAsync(user => user.Id == adminUserId)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (adminUser == null)
        {
            return new DesktopAdminAuthTokenIssueResult(
                false,
                ErrorMessage: "管理员账号不存在或已失效。");
        }

        // 当前桌面 AdminChat 登录协议尚未携带 TenantKey。宁可回退到显式登录，
        // 也不能把租户 Cookie 身份换成缺少租户边界的 JWT。
        if (adminUser.TenantId > 0)
        {
            return new DesktopAdminAuthTokenIssueResult(
                false,
                ErrorMessage: "多租户管理员暂不支持 WebView 自动授权，请使用显式登录。");
        }

        var token = await _adminUserInfoService
            .GenerateTokenForDesktopHandoffAsync(adminUser.Id, sourceAuthenticationExpiresUtc)
            .ConfigureAwait(false);
        return new DesktopAdminAuthTokenIssueResult(
            true,
            adminUser.UserName,
            token.Token,
            token.ExpiresUtc);
    }
}
