using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminChatSessionModuleRepository : IClientRepositoryBase<AdminChatSessionModule>
    {
    }

    public class AdminChatSessionModuleRepository : ClientRepositoryBase<AdminChatSessionModule>, IAdminChatSessionModuleRepository
    {
        private AdminChatSessionModuleRepository() : base(null) { }

        public AdminChatSessionModuleRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}
