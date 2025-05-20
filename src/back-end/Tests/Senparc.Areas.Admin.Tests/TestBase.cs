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
    public class AdminSeedData : UnitTestSeedDataBuilder
    {
        public override async Task<DataList> ExecuteAsync(IServiceProvider serviceProvider)
        {
            DataList dataList = new DataList(nameof(AdminSeedData));
            var seedData = new List<AdminUserInfo>();
            Random rand = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var username = $"Admin-{i}";
                var password = $"pWd-{i}";
                var realName = $"Admin{rand.Next(10000)}";

                var dto = new CreateOrUpdate_AdminUserInfoDto()
                {
                    UserName = username,
                    Password = password,
                    RealName = realName
                };
                var adminUserInfo = new AdminUserInfo(dto);
                seedData.Add(adminUserInfo);
            }
            dataList.Add(seedData);
            return dataList;
        }

        public override async Task OnExecutedAsync(IServiceProvider serviceProvider, DataList dataList)
        {
        }
    }

    public class TestBase : BaseNcfUnitTest
    {
        public TestBase() : this(null, null)
        {
        }

        public TestBase(Action<IServiceCollection> servicesRegister = null, UnitTestSeedDataBuilder seedDataBuilder = null)
            : base(servicesRegister, seedDataBuilder ?? new AdminSeedData())
        {

        }
    }
}