using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminChatMessageRepository : IClientRepositoryBase<AdminChatMessage>
    {
    }

    public class AdminChatMessageRepository : ClientRepositoryBase<AdminChatMessage>, IAdminChatMessageRepository
    {
        private AdminChatMessageRepository() : base(null) { }

        public AdminChatMessageRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
