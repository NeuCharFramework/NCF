using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Tests;

namespace Senparc.Areas.Admin.Domain.Tests
{
    [TestClass()]
    public class AdminUserInfoServiceTests : TestBase
    {
        AdminUserInfoService _adminUserInfoService;

        public AdminUserInfoServiceTests()
        {
            _adminUserInfoService = base._serviceProvider.GetRequiredService<AdminUserInfoService>();
        }

        [TestMethod()]
        public async Task GetUserInfoAsyncTest()
        {

            var dataList = base.dataLists.GetDataList<AdminUserInfo>();
            Console.WriteLine("dataList Count:" + dataList.Count);
            var expected = dataList.FirstOrDefault(z => z.UserName.StartsWith("Admin"));

            var userName = dataList.FirstOrDefault().UserName;
            var result = await _adminUserInfoService.GetUserInfoAsync(userName);
            Assert.AreEqual(expected.UserName, result.UserName);

            result = await _adminUserInfoService.GetUserInfoAsync(userName + "  ");
            Assert.AreEqual(expected.UserName, result.UserName);

            result = await _adminUserInfoService.GetUserInfoAsync(" " + userName);
            Assert.AreEqual(expected.UserName, result.UserName);

            result = await _adminUserInfoService.GetUserInfoAsync(" " + userName + "  ");
            Assert.AreEqual(expected.UserName, result.UserName);

            result = await _adminUserInfoService.GetUserInfoAsync("admin");
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetListTest()
        {
            var result = await _adminUserInfoService.GetObjectListAsync(0, 0, z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Descending, null);
            Assert.IsNotNull(result);

            var dataKeys = base.dataLists.Keys;
            Assert.IsTrue(dataKeys.ToList().Exists(z => z == typeof(AdminUserInfo)));
            Assert.AreEqual(1000, result.Count);
        }
    }
}