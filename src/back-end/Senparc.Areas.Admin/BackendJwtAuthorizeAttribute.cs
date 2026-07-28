/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BackendJwtAuthorizeAttribute.cs
    文件功能描述：BackendJwtAuthorizeAttribute.cs 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Areas.Admin
{
    /// <summary>
    /// 权限认证
    /// </summary>
    public class BackendJwtAuthorizeAttribute: AuthorizeAttribute
    {
        public const string AuthenticationScheme = "Bearer_Backend";
        public const string SuperAdminPolicyName = "AdminSuperAdmin";

        //public const string PolicyName = "backend";

        public BackendJwtAuthorizeAttribute(string policy = null)
        {
            AuthenticationSchemes = AuthenticationScheme;
            Policy = policy;
        }
    }
}
