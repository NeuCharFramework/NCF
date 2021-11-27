﻿using Senparc.Core.Models;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Repository;


namespace Senparc.Service.ACL
{
    public interface IAccountPayLogRepository : IClientRepositoryBase<AccountPayLog>
    {
    }

    public class AccountPayLogRepository : ClientRepositoryBase<AccountPayLog>, IAccountPayLogRepository
    {
        public AccountPayLogRepository(INcfClientDbData NcfClientDbData) : base(NcfClientDbData)
        {

        }
    }
}
