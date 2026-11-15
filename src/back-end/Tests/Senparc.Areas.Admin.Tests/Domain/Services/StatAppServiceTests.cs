using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Senparc.Areas.Admin.OHS.Local.AppService;
using Senparc.CO2NET;
using Senparc.CO2NET.ApiBind;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
[DoNotParallelize]
public class StatAppServiceTests
{
    [TestMethod]
    public async Task MetricsApis_ShouldGenerateGetRoutesAndKeepAdminPolicy()
    {
        var type = typeof(StatAppService);
        Assert.AreEqual(NcfAuthorizationPolicyNames.AdminOnly,
            type.GetCustomAttribute<AdminAuthorizeAttribute>()?.Policy);
        var category = type.Assembly.GetName().Name!;
        var bindings = new ApiBindInfoCollection();
        var names = new[] { nameof(StatAppService.GetHostMetrics), nameof(StatAppService.GetResourceMonitor) };
        foreach (var name in names)
        {
            var method = type.GetMethod(name)!;
            var attributes = method.GetCustomAttributes<ApiBindAttribute>().ToArray();
            Assert.AreEqual(1, attributes.Length, $"{name} must have exactly one API binding.");
            bindings.Add(ApiBindOn.Method, category, method, attributes[0]);
        }

        var engine = new WebApiEngine(options =>
        {
            options.DefaultRequestMethod = ApiRequestMethod.Get;
            options.CopyCustomAttributes = true;
        });
        WebApiEngine.ApiAssemblyNames.TryGetValue(category, out var originalAssemblyName);
        try
        {
            WebApiEngine.ApiAssemblyNames[category] = "StatApiRegression_" + Guid.NewGuid().ToString("N");
            Assert.AreEqual(2, await engine.BuildWebApi(bindings.GroupBy(z => z.Value.Category).Single()));
            var controller = engine.GetApiAssembly(category).GetTypes()
                .Single(z => typeof(ControllerBase).IsAssignableFrom(z));
            foreach (var name in names)
            {
                var action = controller.GetMethods().Single(z =>
                    z.GetCustomAttribute<RouteAttribute>()?.Template?.TrimStart('/') ==
                    $"api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.{name}");
                var verbs = action.GetCustomAttributes().OfType<IActionHttpMethodProvider>()
                    .SelectMany(attribute => attribute.HttpMethods).ToArray();
                Assert.IsTrue(verbs.Length == 0 || verbs.Contains("GET", StringComparer.OrdinalIgnoreCase),
                    $"{name} must accept GET requests.");
            }
        }
        finally
        {
            if (originalAssemblyName == null)
            {
                WebApiEngine.ApiAssemblyNames.TryRemove(category, out _);
            }
            else
            {
                WebApiEngine.ApiAssemblyNames[category] = originalAssemblyName;
            }
        }
    }
}
