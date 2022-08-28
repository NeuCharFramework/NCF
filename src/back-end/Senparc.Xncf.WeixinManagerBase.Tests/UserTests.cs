using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.NeuChar.App.AppStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerBase.Tests
{
    [TestClass]
    public class UserTests : TestBase
    {
        //init

        //private Viewer GenerateNewViewer(string userName = null, string password = null)
        //{
        //    userName ??= new Random().Next(1000).ToString();
        //    password ??= (string)null;

        //    var viewer = new Viewer(userName, password);
        //    return viewer;
        //}

        //[TestMethod]
        //public void UserCreateTest()
        //{
        //    var userName = new Random().Next(1000).ToString();
        //    var password = (string)null;

        //    var viewer = GenerateNewViewer(userName, password);

        //    Assert.AreEqual(userName, viewer.UserName);
        //    Assert.AreEqual(password, viewer.Password);

        //    //Assert.AreEqual(false, viewer.GetType().GetProperty(nameof(viewer.UserName)));

        //    //int result = viewer.UpdatePassword(password);
        //}

        //[TestMethod]
        //public void ChangePassword()
        //{
        //    var viewer = GenerateNewViewer();

        //    var newPassword = Guid.NewGuid().ToString("n");
        //    Operation result = viewer.UpdatePassword(newPassword);

        //    Assert.AreEqual(Operation.Success, result);
        //    Assert.AreEqual(newPassword, viewer.Password);
        //}
    }
}
