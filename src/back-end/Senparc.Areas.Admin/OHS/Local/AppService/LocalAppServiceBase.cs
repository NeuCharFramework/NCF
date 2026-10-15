/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LocalAppServiceBase.cs
    文件功能描述：LocalAppServiceBase.cs implementation and project behavior.


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfModuleManager.Domain.Services;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// 基类
    /// </summary>
    public class LocalAppServiceBase : AppServiceBase
    {
        public LocalAppServiceBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        /// <summary>
        /// 获取当前用的Id
        /// </summary>
        /// <returns></returns>
        public int GetCurrentAdminUserInfoId(HttpContext httpContext = null)
        {
            IEnumerable<Claim> claims = httpContext == null ? ServiceProvider.GetService<IHttpContextAccessor>().HttpContext.User.Claims : httpContext.User.Claims;
            bool isConvertSucess = int.TryParse(claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int convertId);
            if (isConvertSucess)
            {
                return convertId;
            }
            return -1;
        }

        /// <summary>
        /// 获取当前登录管理员的角色代码列表（Cookie 与 JWT 认证均携带 ClaimTypes.Role）
        /// </summary>
        public List<string> GetCurrentAdminRoleCodes(HttpContext httpContext = null)
        {
            IEnumerable<Claim> claims = httpContext == null ? ServiceProvider.GetService<IHttpContextAccessor>().HttpContext.User.Claims : httpContext.User.Claims;
            return claims
                .Where(c => c.Type == ClaimTypes.Role)
                .SelectMany(c => c.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 判断当前登录管理员是否为超级管理员（拥有系统内置 administrator 角色）
        /// </summary>
        public bool IsCurrentAdminSuperAdmin(HttpContext httpContext = null)
        {
            return GetCurrentAdminRoleCodes(httpContext)
                .Any(role => string.Equals(role, Senparc.Ncf.Service.Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, StringComparison.OrdinalIgnoreCase));
        }

    }
}
