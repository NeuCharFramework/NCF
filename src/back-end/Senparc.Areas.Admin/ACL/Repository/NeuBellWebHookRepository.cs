/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookRepository.cs
    文件功能描述：NeuBell WebHook（WebAPI）通知设置仓储

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL.Repository;

public interface INeuBellWebHookRepository : IClientRepositoryBase<NeuBellWebHook> { }

public sealed class NeuBellWebHookRepository : ClientRepositoryBase<NeuBellWebHook>, INeuBellWebHookRepository
{
    private NeuBellWebHookRepository() : base(null) { }
    public NeuBellWebHookRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public interface INeuBellWebHookLogRepository : IClientRepositoryBase<NeuBellWebHookLog> { }

public sealed class NeuBellWebHookLogRepository : ClientRepositoryBase<NeuBellWebHookLog>, INeuBellWebHookLogRepository
{
    private NeuBellWebHookLogRepository() : base(null) { }
    public NeuBellWebHookLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}
