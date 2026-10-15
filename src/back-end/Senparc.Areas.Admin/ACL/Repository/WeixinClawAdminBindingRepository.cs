/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawAdminBindingRepository.cs
    文件功能描述：WeixinClawAdminBindingRepository.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL.Repository;

public interface IWeixinClawAdminBindingRepository : IClientRepositoryBase<WeixinClawAdminBinding>
{
}

public sealed class WeixinClawAdminBindingRepository
    : ClientRepositoryBase<WeixinClawAdminBinding>, IWeixinClawAdminBindingRepository
{
    private WeixinClawAdminBindingRepository() : base(null)
    {
    }

    public WeixinClawAdminBindingRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}
