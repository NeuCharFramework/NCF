/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminAuthConfigRepository.cs
    文件功能描述：AdminAuthConfigRepository 相关功能实现
    
    
    创建标识：Senparc - 20260705
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminAuthConfigRepository : IClientRepositoryBase<AdminAuthConfig>
    {
    }

    public class AdminAuthConfigRepository : ClientRepositoryBase<AdminAuthConfig>, IAdminAuthConfigRepository
    {
        private AdminAuthConfigRepository() : base(null)
        {
        }

        public AdminAuthConfigRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
