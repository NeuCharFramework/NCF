using Senparc.Ncf.Repository;
using Senparc.Xncf.Accounts.Domain.Models;
using System;

namespace Senparc.Xncf.Accounts.Domain.Services
{
    public class AccountOperationLogService : BaseClientService<AccountOperationLog>
    {
        public AccountOperationLogService(ClientRepositoryBase<AccountOperationLog> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
    }
}