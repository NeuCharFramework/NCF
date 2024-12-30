using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Xncf.Accounts.Domain.Models;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Accounts.Tests
{
    [TestClass]
    public class AccountTests : TestBase
    {
        AccountService accountService;

        public AccountTests() : base(null, null)
        {
            accountService = base._serviceProvider.GetRequiredService<AccountService>();
        }

        [TestMethod]
        public async Task CreateAccountTest()
        {
            var accountDto = new CreateOrUpdate_AccountDto()
            {
                UserName = "TestUser",
                Password = "202CB962AC59075B964B07152D234B70", // MD5 hash of "123"
                NickName = "TestNick",
                Email = "test@example.com",
                Phone = "13800138000",
                RealName = "Test User"
            };

            var obj = await accountService.CreateAccountAsync(accountDto);

            // 基本属性验证
            // 验证对象不为空
            Assert.IsNotNull(obj);
            // 验证用户名是否正确匹配
            Assert.AreEqual(accountDto.UserName, obj.UserName);
            // 验证昵称是否正确匹配 
            Assert.AreEqual(accountDto.NickName, obj.NickName);
            // 验证邮箱是否正确匹配
            Assert.AreEqual(accountDto.Email, obj.Email);
            // 验证手机号是否正确匹配
            Assert.AreEqual(accountDto.Phone, obj.Phone);
            // 验证真实姓名是否正确匹配
            Assert.AreEqual(accountDto.RealName, obj.RealName);
            // 验证密码是否正确匹配
            Assert.AreEqual(accountDto.Password, obj.Password);
            // 验证ID是否已正确生成（大于0）
            Assert.IsTrue(obj.Id > 0);
            Console.WriteLine("OBJ ID:" + obj.Id);

            // 密码验证（假设使用相同的 SHA512 加密方式）
            // 密码验证（假设使用相同的 SHA512 加密方式）
            var storedPassword = obj.GetSHA512Password(accountDto.Password, obj.PasswordSalt);
            Assert.AreEqual(storedPassword, obj.Password);
            Console.WriteLine("Stored Password:" + storedPassword);
            Console.WriteLine("Password:" + obj.Password);
            Console.WriteLine("PasswordSalt:" + obj.PasswordSalt);
            Console.WriteLine("PasswordHash:" + obj.PasswordHash);

            // ID 验证
            Assert.IsTrue(obj.Id > 0);
        }

        [TestMethod]
        public async Task GetAccountTest()
        {
            // 假设数据库中已存在用户名为 "Account-100" 的账户
            var obj = await accountService.GetAccountAsync("Account-100");
            Assert.IsNotNull(obj);

            var dataset = BaseNcfUnitTest.GlobalDataList.GetDataList<Account>();
            var data = dataset.Skip(100).Take(1).First();
            Assert.AreEqual(data.UserName, obj.UserName);
        }

        [TestMethod]
        public async Task UpdateAccountTest()
        {
            var newNickName = "UpdatedNick_" + DateTime.Now.Ticks;
            var newEmail = $"updated_{DateTime.Now.Ticks}@example.com";

            var account = await accountService.GetAccountAsync("Account-100");
            Assert.IsNotNull(account);

            var updateDto = new CreateOrUpdate_AccountDto()
            {
                Id = account.Id,
                UserName = account.UserName,
                NickName = newNickName,
                Email = newEmail
            };

            var updatedObj = await accountService.UpdateAccountAsync(updateDto);

            Assert.IsNotNull(updatedObj);
            Assert.AreEqual(newNickName, updatedObj.NickName);
            Assert.AreEqual(newEmail, updatedObj.Email);
        }
    }
} 