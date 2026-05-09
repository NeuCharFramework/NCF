using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminChatSessionRepository : IClientRepositoryBase<AdminChatSession>
    {
    }

    public class AdminChatSessionRepository : ClientRepositoryBase<AdminChatSession>, IAdminChatSessionRepository
    {
        private AdminChatSessionRepository() : base(null) { }

        public AdminChatSessionRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
