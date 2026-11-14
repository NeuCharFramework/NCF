using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Localization;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminMenuLocalizationServiceTests
{
    [TestMethod]
    public void BuiltInMenu_ShouldResolveFromStableId()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            using var provider = new ServiceCollection()
                .AddNcfLocalization()
                .BuildServiceProvider();
            var localizer = provider.GetRequiredService<IStringLocalizer<AdminResource>>();
            var resolver = new AdminMenuLocalizationService(localizer);
            var menu = new SysMenuDto(
                true,
                "2",
                "扩展模块",
                null,
                null,
                null,
                1,
                true,
                null);

            Assert.AreEqual("Extension modules", resolver.Resolve(menu));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [TestMethod]
    public void UserDefinedMenu_ShouldKeepStoredText()
    {
        using var provider = new ServiceCollection()
            .AddNcfLocalization()
            .BuildServiceProvider();
        var localizer = provider.GetRequiredService<IStringLocalizer<AdminResource>>();
        var resolver = new AdminMenuLocalizationService(localizer);
        var menu = new SysMenuDto(
            true,
            "custom-1",
            "用户自定义菜单",
            null,
            "/custom",
            null,
            1,
            true,
            null);

        Assert.AreEqual("用户自定义菜单", resolver.Resolve(menu));
    }
}
