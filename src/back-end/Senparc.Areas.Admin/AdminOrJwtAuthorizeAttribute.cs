using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Config;

namespace Senparc.Areas.Admin
{
    /// <summary>
    /// 兼容 Cookie 和 JWT 认证的权限认证属性
    /// 支持两种认证方式：NcfAdminAuthorizeScheme (Cookie) 或 Bearer_Backend (JWT)
    /// 只要其中一种认证通过即可访问
    /// </summary>
    public class AdminOrJwtAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// JWT 认证方案名称
        /// </summary>
        public const string JwtScheme = "Bearer_Backend";

        /// <summary>
        /// 构造函数，配置支持两种认证方案
        /// </summary>
        public AdminOrJwtAuthorizeAttribute()
        {
            // 用逗号分隔多个认证方案，只要其中一个通过即可
            // NcfAdminAuthorizeScheme (Cookie 认证) + Bearer_Backend (JWT 认证)
            AuthenticationSchemes = $"{SiteConfig.NcfAdminAuthorizeScheme},{JwtScheme}";
        }

        /// <summary>
        /// 构造函数，配置支持两种认证方案并指定 Policy
        /// </summary>
        /// <param name="policy">授权策略</param>
        public AdminOrJwtAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
