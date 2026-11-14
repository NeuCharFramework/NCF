using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Senparc.Areas.Admin.Tests;

[TestClass]
public class AdminDocHelpAssetTests
{
    [TestMethod]
    public async Task EmbeddedHelpAssets_RenderVersionedUrlsMatchingServedContent()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production
        });
        builder.Logging.ClearProviders();
        builder.Services.AddLocalization();
        builder.Services.AddRazorPages().AddApplicationPart(typeof(Register).Assembly);
        await using var application = builder.Build();
        application.Urls.Add("http://127.0.0.1:0");
        new Register().UseXncfModule(application, null!);
        await application.StartAsync();
        try
        {
            using var scope = application.Services.CreateScope();
            var services = scope.ServiceProvider;
            var versions = services.GetRequiredService<IFileVersionProvider>();
            var address = application.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            foreach (var path in new[]
            {
                "/js/Admin/Shared/NcfDocHelpCatalog.js", "/js/Admin/Shared/NcfDocHelp.js",
                "/js/Admin/Shared/NcfDocHelpStandalone.js", "/css/Admin/Shared/NcfDocHelp.css",
                "/css/Admin/Pages/AdminChat/Chat.css"
            })
            {
                var versioned = versions.AddFileVersionToPath(PathString.Empty, path);
                StringAssert.StartsWith(versioned, path + "?v=");
                var response = await client.GetAsync(versioned);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var content = await response.Content.ReadAsByteArrayAsync();
                Assert.AreEqual(path + "?v=" + WebEncoders.Base64UrlEncode(SHA256.HashData(content)), versioned);
                Assert.AreEqual("/mounted" + versioned,
                    versions.AddFileVersionToPath("/mounted", "/mounted" + path));
                if (path.EndsWith("NcfDocHelpCatalog.js", StringComparison.Ordinal))
                {
                    var catalog = Encoding.UTF8.GetString(content);
                    StringAssert.Contains(catalog, "'admin.memory':");
                    StringAssert.Contains(catalog, "'admin.memory.entries':");
                }
            }

            var httpContext = new DefaultHttpContext { RequestServices = services };
            httpContext.Request.PathBase = "/mounted";
            var routeData = new RouteData();
            routeData.Routers.Add(new RouteCollection());
            var action = new ActionContext(httpContext, routeData, new ActionDescriptor());
            var engine = services.GetRequiredService<IRazorViewEngine>();
            var result = engine.GetView(null, "/Areas/Admin/Pages/Shared/_DocHelpStandalone.cshtml", false);
            Assert.IsTrue(result.Success, "Compiled standalone help partial must be available.");
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            var viewData = new ViewDataDictionary<string>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = "admin.memory"
            };
            var context = new ViewContext(action, result.View, viewData,
                new TempDataDictionary(httpContext, services.GetRequiredService<ITempDataProvider>()),
                writer, new HtmlHelperOptions());
            await result.View.RenderAsync(context);
            var html = writer.ToString();
            foreach (var asset in new[] { "NcfDocHelp.css", "NcfDocHelpCatalog.js", "NcfDocHelp.js", "NcfDocHelpStandalone.js" })
            {
                Assert.IsTrue(Regex.IsMatch(html, @"/mounted/[^""<>]*" + Regex.Escape(asset) + @"\?v=[\w-]+"),
                    "Rendered help URL must contain a real content hash: " + asset);
            }
        }
        finally
        {
            await application.StopAsync();
        }
    }

    [TestMethod]
    public async Task PhysicalOverrides_RemainFirstForServingAndVersioning()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncf-help-assets-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "js", "Admin", "Shared");
        Directory.CreateDirectory(path);
        const string content = "// Physical site override";
        await File.WriteAllTextAsync(Path.Combine(path, "NcfDocHelpCatalog.js"), content);
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory,
                WebRootPath = root,
                EnvironmentName = Environments.Production
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRazorPages();
            await using var application = builder.Build();
            application.Urls.Add("http://127.0.0.1:0");
            application.UseStaticFiles();
            new Register().UseXncfModule(application, null!);
            await application.StartAsync();
            try
            {
                using var scope = application.Services.CreateScope();
                var versions = scope.ServiceProvider.GetRequiredService<IFileVersionProvider>();
                const string url = "/js/Admin/Shared/NcfDocHelpCatalog.js";
                var versioned = versions.AddFileVersionToPath(PathString.Empty, url);
                Assert.AreEqual(url + "?v=" + WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(content))), versioned);
                var address = application.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var client = new HttpClient { BaseAddress = new Uri(address) };
                Assert.AreEqual(content, await client.GetStringAsync(versioned));
            }
            finally
            {
                await application.StopAsync();
            }
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
