/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatSessionWorkflowRepository.cs
    文件功能描述：AdminChatSessionWorkflowRepository.cs 相关实现


    创建标识：Senparc - 20260821

    修改标识：Senparc - 20260822
    修改描述：v0.6.0 新增管理端 Chat 会话工作流能力

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface IAdminChatSessionWorkflowRepository : IClientRepositoryBase<AdminChatSessionWorkflow>
{
}

public sealed class AdminChatSessionWorkflowRepository
    : ClientRepositoryBase<AdminChatSessionWorkflow>, IAdminChatSessionWorkflowRepository
{
    private AdminChatSessionWorkflowRepository() : base(null!) { }

    public AdminChatSessionWorkflowRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}
