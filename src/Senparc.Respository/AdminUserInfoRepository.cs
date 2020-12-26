using Senparc.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Repository
{
    public interface IAdminUserInfoRepository : IClientRepositoryBase<AdminUserInfo>
    {
    }

    public class AdminUserInfoRepository : ClientRepositoryBase<AdminUserInfo>, IAdminUserInfoRepository
    {
        public AdminUserInfoRepository(INcfClientDbData NcfClientDbData) : base(NcfClientDbData)
        {

        }
    }
}

