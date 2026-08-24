using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Senparc.Ncf.AreaBase.Admin.Filters;

namespace Senparc.Areas.Admin.Tests;

[TestClass]
public class AdminAuthenticationConfigurationTests
{
    [TestMethod]
    public void AuthorizeConfig_AllowsLocalHttpLoginWithoutWeakeningHttpsCookies()
    {
        var services = new ServiceCollection();
        var mvcBuilder = services.AddRazorPages();
        var environment = new Mock<IHostEnvironment>().Object;

        new Senparc.Areas.Admin.Register().AuthorizeConfig(mvcBuilder, environment);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(AdminAuthorizeAttribute.AuthenticationScheme);

        Assert.IsTrue(options.Cookie.HttpOnly);
        Assert.IsTrue(options.Cookie.IsEssential);
        Assert.AreEqual(SameSiteMode.Strict, options.Cookie.SameSite);
        Assert.AreEqual(CookieSecurePolicy.SameAsRequest, options.Cookie.SecurePolicy);

        var authOptions = serviceProvider.GetRequiredService<IOptionsMonitor<AuthenticationOptions>>().CurrentValue;
        Assert.AreEqual(AdminAuthorizeAttribute.AuthenticationScheme, authOptions.DefaultScheme);
        Assert.AreEqual(AdminAuthorizeAttribute.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);
        Assert.AreEqual(AdminAuthorizeAttribute.AuthenticationScheme, authOptions.DefaultChallengeScheme);
        Assert.AreEqual(AdminAuthorizeAttribute.AuthenticationScheme, authOptions.DefaultForbidScheme);
    }
}
