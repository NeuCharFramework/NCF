/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminAuthConfig.cs
    文件功能描述：AdminAuthConfig 相关功能实现
    
    
    创建标识：Senparc - 20260705
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(AdminAuthConfig))]
    [Serializable]
    public class AdminAuthConfig : EntityBase<int>
    {
        public const int DefaultAdminWebLoginExpireMinutes = 120;
        public const int DefaultBackendJwtExpireMinutes = 150 * 60;

        public int AdminWebLoginExpireMinutes { get; private set; }
        public int BackendJwtExpireMinutes { get; private set; }

        private AdminAuthConfig()
        {
        }

        public AdminAuthConfig(int adminWebLoginExpireMinutes, int backendJwtExpireMinutes)
        {
            AdminWebLoginExpireMinutes = adminWebLoginExpireMinutes > 0
                ? adminWebLoginExpireMinutes
                : DefaultAdminWebLoginExpireMinutes;
            BackendJwtExpireMinutes = backendJwtExpireMinutes > 0
                ? backendJwtExpireMinutes
                : DefaultBackendJwtExpireMinutes;
        }

        public void Update(int adminWebLoginExpireMinutes, int backendJwtExpireMinutes)
        {
            AdminWebLoginExpireMinutes = adminWebLoginExpireMinutes > 0
                ? adminWebLoginExpireMinutes
                : DefaultAdminWebLoginExpireMinutes;
            BackendJwtExpireMinutes = backendJwtExpireMinutes > 0
                ? backendJwtExpireMinutes
                : DefaultBackendJwtExpireMinutes;
            SetUpdateTime();
        }
    }
}
