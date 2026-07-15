/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Session.cshtml.cs
    文件功能描述：Session.cshtml 相关功能实现
    
    
    创建标识：Senparc - 20260705
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Config;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [ServiceFilter(typeof(AuthenticationResultFilterAttribute))]
    [AdminOrJwtAuthorize("AdminOnly")]
    public class SessionModel(AdminUserInfoService adminUserInfoService) : BasePageModel
    {
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;

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
                    return Ok(false, "当前用户身份无效，无法续期 JWT。请重新登录。");
                }

                var token = _adminUserInfoService.GenerateToken(userId, out var expiresUtc, jwtExpireMinutes);
                return Ok(new
                {
                    authType = "jwt",
                    serverUtc = now,
                    expiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes,
                    token
                });
            }

            return Unauthorized();
        }
    }
}
