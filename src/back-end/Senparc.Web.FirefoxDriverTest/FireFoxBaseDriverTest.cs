using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using System;
using System.Linq;
using System.Threading;

namespace Senparc.Web.FirefoxDriverTest
{
    /// <summary>
    /// ui自动化测试基类
    /// </summary>
    [TestClass]
    public class FireFoxBaseDriverTest
    {
        // In order to run the below test(s), 
        // please follow the instructions from https://github.com/mozilla/geckodriver/releases
        // to install Firefox WebDriver.

        protected RemoteWebDriver _driver;

        /// <summary>
        /// 截图路径
        /// </summary>
        private string screenShotPath;//"F:\\auto-test\\ncf";

        /// <summary>
        /// 步骤
        /// </summary>
        private int step = 0;

        /// <summary>
        /// NCF项目地址
        /// </summary>
        const string webSite = "https://localhost:44311/Admin/Login";

        [TestInitialize]
        public virtual void EdgeDriverInitialize()
        {
            // Initialize firefox driver 
            System.Text.CodePagesEncodingProvider.Instance.GetEncoding(437);
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);//注册编码
            var options = new FirefoxOptions
            {
                PageLoadStrategy = PageLoadStrategy.Normal,
                AcceptInsecureCertificates = true//忽略ssl警告
            };
            screenShotPath = System.IO.Path.Combine(Environment.CurrentDirectory, "screenShot\\", DateTime.Now.ToString("yyyy年MM月dd日HH时mm分"));
            if (!System.IO.Directory.Exists(screenShotPath))
            {
                System.IO.Directory.CreateDirectory(screenShotPath);
            }
            _driver = new FirefoxDriver(options);
            loadPage();
        }

        /// <summary>
        /// 初始化页面
        /// </summary>
        private void loadPage() => _driver.Url = webSite;

        /// <summary>
        /// 测试
        /// </summary>
        [TestMethod]
        public void VerifyPageTitle()
        {
            captureScreenShot("登录页面");
            Assert.AreEqual("登录 NCF 管理后台", _driver.Title);
        }

        /// <summary>
        /// 验证登录逻辑
        /// </summary>
        [TestMethod]
        public void VerifyLoginModule()
        {
            _verifyLogin();
        }

        /// <summary>
        /// 验证登录逻辑
        /// </summary>
        public void _verifyLogin()
        {
            string userName = "SenparcCoreAdmin36";//登录名
            string pwd = "123456";//登陆密码
            IWebElement loginInput = _driver.FindElementByCssSelector("div.el-form-item:nth-child(1) > div:nth-child(1) > div:nth-child(1) > input:nth-child(1)");//用户名输入框
            IWebElement pwdInput = _driver.FindElementByCssSelector("div.el-form-item:nth-child(2) > div:nth-child(1) > div:nth-child(1) > input:nth-child(1)");//密码输入框
            IWebElement loginBtn = _driver.FindElementByCssSelector(".el-button");//登录按钮
            loginInput.SendKeys(userName);
            pwdInput.SendKeys(pwd);
            captureScreenShot();
            loginBtn.Click();//登录按钮click事件
            captureScreenShot();
            Assert.AreEqual("管理员后台首页", _driver.Title);
        }

        [TestCleanup]
        public void EdgeDriverCleanup()
        {
            _driver.Quit();
        }

        /// <summary>
        /// 截图并保存
        /// </summary>
        /// <param name="stepName"></param>
        protected void captureScreenShot(string stepName = "")
        {
            Screenshot screenShot = _driver.GetScreenshot();
            Interlocked.Increment(ref step);
            screenShot.SaveAsFile(string.Concat(screenShotPath, "\\step_", step, '_', stepName, ".png"), ScreenshotImageFormat.Png);
        }
    }
}
