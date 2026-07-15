/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminAuthConfig_Request.cs
    文件功能描述：AdminAuthConfig_Request 相关功能实现
    
    
    创建标识：Senparc - 20260705
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.OHS.PL
{
    public class AdminAuthConfig_SetExpireSettingsRequest : FunctionAppRequestBase
    {
        [Range(5, 525600)]
        public int AdminWebLoginExpireMinutes { get; set; } = AdminAuthConfig.DefaultAdminWebLoginExpireMinutes;

        [Range(5, 525600)]
        public int BackendJwtExpireMinutes { get; set; } = AdminAuthConfig.DefaultBackendJwtExpireMinutes;
    }
}
