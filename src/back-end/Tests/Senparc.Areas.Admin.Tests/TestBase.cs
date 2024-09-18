using Microsoft.Extensions.DependencyInjection;
using Moq;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using System.Linq.Expressions;

namespace Senparc.Areas.Admin.Tests
{
    public class TestBase : BaseNcfUnitTest
    {
        private static bool initSeedDataFinish = false;

        /// <summary>
        /// 创建种子数据
        /// </summary>
        private static Action<DataList> InitSeedData = dataList =>
        {
            if (initSeedDataFinish)
            {
                return;
            }

            List<AdminUserInfo> users = new List<AdminUserInfo>();
            string[] userNames = new string[] { "Admin", "User1", "User2", "User3" };
            foreach (string user in userNames)
            {
                var userName = user;
                var pwd = user + "@";
                var adminUserInfo = new AdminUserInfo(ref userName, ref pwd, user, "", "");
                users.Add(adminUserInfo);
            }

            dataList[typeof(AdminUserInfo)] = users.Cast<object>().ToList();
        };

        public TestBase() : this(null, InitSeedData)
        {
        }

        public TestBase(Action<IServiceCollection> servicesRegister = null, Action<DataList> initSeedData = null) : base(servicesRegister, initSeedData)
        {

        }
    }
}