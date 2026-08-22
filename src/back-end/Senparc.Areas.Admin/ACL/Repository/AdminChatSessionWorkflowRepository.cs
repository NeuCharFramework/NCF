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
