using Senparc.Areas.Admin.Tests;

namespace Senparc.Areas.Admin.Domain.Tests
{
    [TestClass()]
    public class AdminUserInfoServiceTests : TestBase
    {

        public AdminUserInfoServiceTests()
        {
        }


        [TestMethod()]
        public async Task GetUserInfoAsyncTest()
        {
            var adminUserInfoService = new AdminUserInfoService(mockAdminUserInfoRepository.Object, null, base._serviceProvider);

            var userName = "Admin";

            var result = await adminUserInfoService.GetUserInfoAsync(userName);
            Assert.AreEqual(_adminUserInfoList[0], result);

            result = await adminUserInfoService.GetUserInfoAsync(userName + "  ");
            Assert.AreEqual(_adminUserInfoList[0], result);

            result = await adminUserInfoService.GetUserInfoAsync(" " + userName);
            Assert.AreEqual(_adminUserInfoList[0], result);

            result = await adminUserInfoService.GetUserInfoAsync(" " + userName + "  ");
            Assert.AreEqual(_adminUserInfoList[0], result);

            result = await adminUserInfoService.GetUserInfoAsync("admin");
            Assert.IsNull(result);


        }
    }
}