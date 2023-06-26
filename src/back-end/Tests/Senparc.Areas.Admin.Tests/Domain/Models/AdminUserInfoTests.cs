using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.Areas.Admin.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Models.Tests
{
    [TestClass()]
    public class AdminUserInfoTests
    {
        [TestMethod()]
        public void GetSHA512PasswordTest()
        {
            var userName = "JeffreySU";
            var pwd = "JeffreySu@Pwd";
            var salt = "SenparcSALTmustGreaterThan16digit";
            Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.PasswordSaltToken = "1234567890";

            var adminUserInfo = new AdminUserInfo(ref userName, ref pwd, "Jeffrey", "4000318816", "TestAccount");

            //使用 PasswordSaltToken
            var result = adminUserInfo.GetSHA512Password(pwd, salt, true);
            Console.WriteLine(result);
            Assert.AreEqual("g016e97e8c9b281f8ab4a6cc9ecf04315c6c079df49", result);

            //不使用 PasswordSaltToken
            result = adminUserInfo.GetSHA512Password(pwd, salt, false);
            Console.WriteLine(result);
            Assert.AreEqual("g01f53df7b515d060492e60174ead8560abbaf457aebfa5cc686454b8292a2b5b8b", result);

            //使用 PasswordSaltToken，但是未设置
            Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.PasswordSaltToken = null;
            result = adminUserInfo.GetSHA512Password(pwd, salt, true);
            Console.WriteLine(result);
            Assert.AreEqual("A1BC21E701CF0C70995D3B5360F581D0", result);
        }
    }
}