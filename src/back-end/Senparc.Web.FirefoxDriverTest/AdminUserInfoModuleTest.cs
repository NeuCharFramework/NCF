using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Senparc.Web.FirefoxDriverTest
{
    [TestClass]
    public class AdminUserInfoModuleTest : FireFoxBaseDriverTest
    {
        /// <summary>
        /// 管理员模块
        /// </summary>
        [TestMethod]
        public void VerifyAdminModule()
        {
            _verifyLogin();//登录
            System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> firstNavs = _driver.FindElementsByCssSelector("ul.el-menu:nth-child(1) > li.el-submenu");//一级导航
            IWebElement systemManager_li = firstNavs.FirstOrDefault(_ => "系统管理".Equals(_.FindElement(By.CssSelector("div.el-submenu__title span")).Text));
            systemManager_li.Click();//点击系统管理
            captureScreenShot();
            System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> subNavs_li = systemManager_li.FindElements(By.CssSelector("ul.el-menu li.el-menu-item"));//
            IWebElement adminUserInfoManager_li = subNavs_li.FirstOrDefault(_ => "管理员管理".Equals(_.FindElement(By.CssSelector("span")).Text));
            adminUserInfoManager_li.Click();
            captureScreenShot();
            Assert.AreEqual("管理员管理", _driver.Title, "管理员管理页面标题不符合 “管理员管理”");
            IWebElement userTr = _addAdminUserInfo();
            _setAdminRoel(userTr);
        }

        /// <summary>
        /// 添加管理员
        /// </summary>
        private IWebElement _addAdminUserInfo()
        {
            IWebElement addBtn = _driver.FindElement(By.CssSelector(".filter-item"));
            addBtn.Click();
            captureScreenShot();
            fillAdminUserInfoForm(out string userName);
            _driver.FindElementByCssSelector(".el-dialog .el-dialog__footer button.el-button--primary").Click();//保存按钮
            captureScreenShot();
            System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> trs = _driver.FindElementsByCssSelector(".el-table__body tbody tr");
            IWebElement webElement = trs.FirstOrDefault(_ => userName.Equals(_.FindElement(By.CssSelector("td:nth-child(2) div.cell")).Text));
            Assert.IsNotNull(webElement, $"未找到 {userName} 的用户");
            return webElement;
        }

        /// <summary>
        /// 设置角色
        /// </summary>
        /// <param name="tr"></param>
        private void _setAdminRoel(IWebElement tr)
        {
            tr.FindElement(By.CssSelector("td:nth-child(5) div.cell button:nth-child(2)")).Click();//设置角色
            captureScreenShot();
            IWebElement[] checkBoxs = _driver.FindElementsByCssSelector(".el-dialog .el-dialog__body .el-checkbox-group label").ToArray();
            ICollection<string> checkedRole = new List<string>();
            int checkCount = new Random().Next(1, checkBoxs.Length);//选中的数量，至少选择一个
            for (int i = 0; i < checkCount; i++)
            {
                int checkIndex = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray())).Next(0, checkBoxs.Length);//重复点击问题
                checkBoxs[checkIndex].Click();
                checkedRole.Add(checkBoxs[checkIndex].Text);
            }
            checkedRole = checkedRole.Distinct().ToList();
            checkCount = checkedRole.Count();
            Console.WriteLine("输入选中数量：{0}", checkCount);
            captureScreenShot("输入角色");
            _driver.FindElementByCssSelector("div.el-dialog__wrapper:nth-child(5) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > button:nth-child(2)").Click();//保存
            tr.FindElement(By.CssSelector("td:nth-child(5) div.cell button:nth-child(2)")).Click();//设置角色
            captureScreenShot("输出角色");
            checkBoxs = _driver.FindElementsByCssSelector(".el-dialog .el-dialog__body .el-checkbox-group label").ToArray();
            IWebElement[] nowCheckedEle = checkBoxs.Where(_ => _.GetAttribute("class").Contains("is-checked")).ToArray();
            Assert.AreEqual(checkCount, nowCheckedEle.Length, $"选中角色数量不正确，输入角色数量：{checkCount},输出色数量：{nowCheckedEle.Length}");
            IEnumerable<string> outputRole = nowCheckedEle.Select(_ => _.Text);
            int exceptCount = outputRole.Except(checkedRole).Count();
            Assert.AreEqual(0, exceptCount, $"角色选中不正确，输入{string.Join(',', checkedRole)}, 输出{string.Join(',', outputRole)}");
        }

        /// <summary>
        /// 填充管理人员表单
        /// </summary>
        private void fillAdminUserInfoForm(out string userName)
        {
            string random = new Random().Next(100, 1000).ToString();
            string[] data = new string[6]
            {
                "user_" + random,
                "123456",
                "123456",
                "realname_" + random,
                "phone_" + random,
                "note_" + random
            };
            userName = data[0];
            IWebElement[] inputs = _driver.FindElementsByCssSelector(".el-dialog .el-dialog__body form .el-input input").ToArray();
            Assert.AreEqual(6, inputs.Length, $"管理员新增表单input数量大于或者小于6个，当前input数量:{inputs.Length}");
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i].SendKeys(data[i]);
            }
            captureScreenShot();
        }
    }
}
