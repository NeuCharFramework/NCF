using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Core.Models;

namespace Senparc.Service.ACL
{
    public interface IAccountPayLogRepository : IClientRepositoryBase<AccountPayLog>
    {
    }

    public class AccountPayLogRepository : ClientRepositoryBase<AccountPayLog>, IAccountPayLogRepository
    {
        public AccountPayLogRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}

