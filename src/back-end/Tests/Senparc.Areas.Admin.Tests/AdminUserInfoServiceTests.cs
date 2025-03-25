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
using Senparc.Ncf.UnitTestExtension.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Tests
{
    [TestClass]
    public class AdminUserInfoServiceTests : TestBase
    {
        AdminUserInfoService adminUserInfoService;


        public AdminUserInfoServiceTests() : base(null, null)
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

            Assert.IsTrue(obj.Id > 0);
            Console.WriteLine("OBJ ID:" + obj.Id);
        }

        [TestMethod]
        public async Task GetUserInfoAsyncTest()
        {
            var obj = await adminUserInfoService.GetUserInfoAsync("Admin-600");
            Assert.IsNotNull(obj);

            var dataset = BaseNcfUnitTest.GlobalDataList.GetDataList<AdminUserInfo>();
            var data = dataset.Skip(600).Take(1).First();
            Assert.AreEqual(data.UserName, obj.UserName);
        }
    }
}
