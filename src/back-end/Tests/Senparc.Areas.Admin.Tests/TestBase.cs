using Microsoft.Extensions.DependencyInjection;
using Moq;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.UnitTestExtension;
using System.Linq.Expressions;

namespace Senparc.Areas.Admin.Tests
{
    public class TestBase : BaseNcfUnitTest
    {
        protected Mock<IAdminUserInfoRepository> mockAdminUserInfoRepository;
        protected List<AdminUserInfo> _adminUserInfoList = new List<AdminUserInfo>();

        protected void CreateSeedData()
        {
            string[] userNames = new string[] { "Admin", "User1", "User2", "User3" };
            foreach (string user in userNames)
            {
                var userName = user;
                var pwd = user + "@";
                var adminUserInfo = new AdminUserInfo(ref userName, ref pwd, user, "", "");

                _adminUserInfoList.Add(adminUserInfo);
            }
            base.dataLists[typeof(AdminUserInfo)] = _adminUserInfoList.Cast<object>().ToList();

        }

        public TestBase() : this(null, null)
        {
        }

        public TestBase(Action<IServiceCollection> servicesRegister = null, Action<Dictionary<Type, List<object>>> initSeedData = null) : base(servicesRegister, initSeedData)
        {
            CreateSeedData();

            var adminUserInfoRepositoryBase = base.GetRespository<AdminUserInfo>();
            mockAdminUserInfoRepository = base.CreateMockForExtendedInterface<IAdminUserInfoRepository, IClientRepositoryBase<AdminUserInfo>>(adminUserInfoRepositoryBase.MockRepository);
        }
    }
}