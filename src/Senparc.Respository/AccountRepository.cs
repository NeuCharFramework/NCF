using Senparc.Core.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Repository
{
    public interface IAccountRepository : IClientRepositoryBase<Account>
    {
    }

    public class AccountRepository : ClientRepositoryBase<Account>, IAccountRepository
    {
        public AccountRepository(ISqlClientFinanceData sqlClientFinanceData) : base(sqlClientFinanceData)
        {

        }
    }
}

