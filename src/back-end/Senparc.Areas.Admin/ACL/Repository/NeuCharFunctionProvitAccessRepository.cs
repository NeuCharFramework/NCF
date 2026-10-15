/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharFunctionProvitAccessRepository.cs
    文件功能描述：NeuCharFunctionProvitAccess（Function 全局 Provit 数据库访问策略）仓储

    创建标识：Senparc - 20260917
    修改描述：v0.9.1 新增 Function 全局 Provit 数据库访问策略映射

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface INeuCharFunctionProvitAccessRepository : IClientRepositoryBase<NeuCharFunctionProvitAccess> { }

public sealed class NeuCharFunctionProvitAccessRepository : ClientRepositoryBase<NeuCharFunctionProvitAccess>, INeuCharFunctionProvitAccessRepository
{
    private NeuCharFunctionProvitAccessRepository() : base(null) { }
    public NeuCharFunctionProvitAccessRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}
