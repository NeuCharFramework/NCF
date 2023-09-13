using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Accounts.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AccountsTests.Domain.Models
{
    [TestClass]
    public class AccountOperationLogTests:TestBase
    {
        [TestMethod]
        public void CreatTest()
        {
            var note = "这里是备注信息";
            var @operator = "操作人";
            var operateTime = SystemTime.Now.DateTime;

            var accountOperationLog = new AccountOperationLog(note, @operator, operateTime);

            Assert.AreEqual(note, accountOperationLog.Note);
            Assert.AreEqual(@operator, accountOperationLog.Operator);
            Assert.AreEqual(operateTime, accountOperationLog.OperateTime);
        }

        [TestMethod]
        public void ReBuildNoteTest()
        {
            var typeKind = AccountOperationType.Message;
            var note = "这里是备注信息";
            var @operator = "操作人";
            var operateTime = SystemTime.Now.DateTime;

            var accountOperationLog = new AccountOperationLog(note, @operator, operateTime);

            accountOperationLog.ReBuildOperationType(typeKind);

            Assert.AreEqual("[Message]" + note, accountOperationLog.Note);
        }
    }
}
