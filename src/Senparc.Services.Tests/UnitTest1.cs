using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.RegularExpressions;

namespace Senparc.Services.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var dt1 = Current;
            var dt2 = Current;
            System.Console.WriteLine(dt1.Ticks.ToString());
            System.Console.WriteLine(dt2.Ticks.ToString());

            Assert.AreNotEqual(dt1, dt2);
        }
        [TestMethod]
        public void TestMethod2()
        {
            Regex regex = new Regex("^(http|https)://(\\w+(\\.)?)+");
            var match = regex.Match("https://open.weixin.qq.com/connect/oauth2/authorize?appid=wx7988ec84b99e4f7d&redirect_uri=https://scf.senparc.com/Portal/Center/Callback&response_type=code&scope=snsapi_base&state=Center&connect_redirect=1#wechat_redirect");
            if (match.Success)
            {
                Console.WriteLine(match.Value);
            }
            Assert.IsTrue(match.Success);
        }



        public DateTime Current => DateTime.Now;
    }
}
