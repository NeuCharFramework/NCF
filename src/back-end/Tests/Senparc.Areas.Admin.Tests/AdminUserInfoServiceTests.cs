using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Ncf.UnitTestExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Tests
{
    [TestClass]
    public class AdminUserInfoServiceTests : BaseNcfUnitTest
    {

        #region 生成 Seed Data（种子数据）

        private static Action<Dictionary<Type, List<object>>> InitSeedData = seedData =>
        {
            var list = new List<object>();
            Random rand = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var username = $"Admin-{i}";
                var password = $"pWd-{i}";
                var realName = $"Admin{rand.Next(10000)}";
                var adminUserInfo = new AdminUserInfo(ref username, ref password, realName, "", "");
                list.Add(adminUserInfo);
            }
            seedData.Add(typeof(AdminUserInfo), list);
        };

        #endregion

        AdminUserInfoService adminUserInfoService;


        public AdminUserInfoServiceTests() : base(null, InitSeedData)
        {
            adminUserInfoService = base._serviceProvider.GetRequiredService<AdminUserInfoService>();
        }

        [TestMethod]
        public async Task CreateAdminUserInfoTest()
        {
            var adminUserInfoDto = new CreateOrUpdate_AdminUserInfoDto()
            {
                UserName = "NCF_Admin",
                Password = "abcd",
            };

            var obj = await adminUserInfoService.CreateAdminUserInfoAsync(adminUserInfoDto);

            Assert.IsNotNull(obj);
            Assert.AreEqual(adminUserInfoDto.UserName, obj.UserName);

            var storedPassword = obj.GetSHA512Password(adminUserInfoDto.Password, obj.PasswordSalt);
            Assert.AreEqual(storedPassword, obj.Password);
        }

        [TestMethod]
        public async Task GetUserInfoAsyncTest()
        {
            var obj = await adminUserInfoService.GetUserInfoAsync("Admin-600");
            Assert.IsNotNull(obj);

            var dataset = base.dataLists.GetDataList<AdminUserInfo>();
            var data = dataset.Skip(600).Take(1).First();
            Assert.AreEqual(data.UserName, obj.UserName);
        }
    }
}
