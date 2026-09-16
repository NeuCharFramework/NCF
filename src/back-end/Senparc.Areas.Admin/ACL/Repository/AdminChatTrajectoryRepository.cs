/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatTrajectoryRepository.cs
    文件功能描述：AdminChatTrajectoryRepository 相关功能实现


    创建标识：Senparc - 20260915

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface IAdminChatTrajectoryRepository : IClientRepositoryBase<AdminChatTrajectory>
{
}

public class AdminChatTrajectoryRepository : ClientRepositoryBase<AdminChatTrajectory>, IAdminChatTrajectoryRepository
{
    private AdminChatTrajectoryRepository() : base(null) { }

    public AdminChatTrajectoryRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

public interface IAdminChatTrajectoryEventRepository : IClientRepositoryBase<AdminChatTrajectoryEvent>
{
}

public class AdminChatTrajectoryEventRepository : ClientRepositoryBase<AdminChatTrajectoryEvent>, IAdminChatTrajectoryEventRepository
{
    private AdminChatTrajectoryEventRepository() : base(null) { }

    public AdminChatTrajectoryEventRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}
