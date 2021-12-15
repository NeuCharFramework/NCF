using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Core.Models;

namespace Senparc.Service.ACL
{
    public interface IAccountRepository : IClientRepositoryBase<Account>
    {
    }

    public class AccountRepository : ClientRepositoryBase<Account>, IAccountRepository
    {
        public AccountRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}

