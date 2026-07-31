/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Session.cshtml.cs
    文件功能描述：Session.cshtml 相关功能实现
    
    
    创建标识：Senparc - 20260705
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强后台模块批量更新并完善多语言管理界面

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [ServiceFilter(typeof(AuthenticationResultFilterAttribute))]
    [AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class SessionModel(AdminUserInfoService adminUserInfoService, IStringLocalizer<AdminResource> localizer) : BasePageModel
    {
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;
        private readonly IStringLocalizer<AdminResource> _localizer = localizer;

        public async Task<IActionResult> OnGetStatusAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var webLoginExpireMinutes = _adminUserInfoService.GetAdminWebLoginExpireMinutes();
            var jwtExpireMinutes = _adminUserInfoService.GetBackendJwtExpireMinutes();

            var cookieAuthResult = await HttpContext.AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme);
            if (cookieAuthResult?.Succeeded == true && cookieAuthResult.Principal?.Identity?.IsAuthenticated == true)
            {
                var expiresUtc = cookieAuthResult.Properties?.ExpiresUtc ?? now.AddMinutes(webLoginExpireMinutes);
                return Ok(new
                {
                    authType = "cookie",
                    serverUtc = now,
                    expiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes
                });
            }

            if (User?.Identity?.IsAuthenticated == true)
            {
                var expiresUtc = _adminUserInfoService.TryGetJwtExpiresUtc(User) ?? now.AddMinutes(jwtExpireMinutes);
                return Ok(new
                {
                    authType = "jwt",
                    serverUtc = now,
                    expiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes
                });
            }

            return Unauthorized();
        }

        public async Task<IActionResult> OnGetKeepAliveAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var webLoginExpireMinutes = _adminUserInfoService.GetAdminWebLoginExpireMinutes();
            var jwtExpireMinutes = _adminUserInfoService.GetBackendJwtExpireMinutes();

            var cookieAuthResult = await HttpContext.AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme);
            if (cookieAuthResult?.Succeeded == true && cookieAuthResult.Principal?.Identity?.IsAuthenticated == true)
            {
                var expiresUtc = await _adminUserInfoService.KeepCookieLoginAliveAsync(cookieAuthResult.Principal);
                return Ok(new
                {
                    authType = "cookie",
                    serverUtc = now,
                    expiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes,
                    token = string.Empty
                });
            }

            if (User?.Identity?.IsAuthenticated == true)
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
                {
                    return Ok(false, _localizer["Admin.Session.InvalidUser"].Value);
                }

                var tokenResult = await _adminUserInfoService.GenerateTokenAsync(userId, jwtExpireMinutes);
                return Ok(new
                {
                    authType = "jwt",
                    serverUtc = now,
                    expiresUtc = tokenResult.ExpiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes,
                    token = tokenResult.Token
                });
            }

            return Unauthorized();
        }
    }
}
