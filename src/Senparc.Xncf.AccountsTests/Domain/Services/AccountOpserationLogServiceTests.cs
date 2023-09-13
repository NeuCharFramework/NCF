using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Xncf.Accounts.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AccountsTests.Domain.Services
{
    [TestClass]
    public class AccountOpserationLogServiceTests:TestBase
    {
        [TestMethod]
        public async Task CreateTest()
        {
            var note = "这里是备注信息";
            var @operator = "操作人";
            var operateTime = SystemTime.Now.DateTime;

            var accountOperationLog = new AccountOperationLog(note, @operator, operateTime);

            var service = base.ServiceProvider.GetService<AccountOperationLogService>();
           
            await service.SaveObjectAsync(accountOperationLog);
        }
    }
}
