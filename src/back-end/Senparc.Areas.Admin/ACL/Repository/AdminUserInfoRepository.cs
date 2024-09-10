using Senparc.Areas.Admin.Domain.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminUserInfoRepository : IClientRepositoryBase<AdminUserInfo>
    {
    }

    public class AdminUserInfoRepository : ClientRepositoryBase<AdminUserInfo>, IAdminUserInfoRepository
    {
        private AdminUserInfoRepository() : base(null) { }

        public AdminUserInfoRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}

