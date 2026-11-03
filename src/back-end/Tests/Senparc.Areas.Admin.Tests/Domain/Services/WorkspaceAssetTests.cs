using System.IO.Compression;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Moq;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.OHS.Local.Controllers;
using Senparc.Xncf.Workspace;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Authorization;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class WorkspaceAssetTests
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private Mock<IWorkspaceStorage> _storage = null!;
    private Mock<IAdminChatSessionRepository> _repository = null!;
    private ServiceProvider _services = null!;
    private AdminChatWorkspaceAssetService _assets = null!;
    private string _directory = null!;
    private readonly TestTimeProvider _clock = new();
    private readonly EphemeralDataProtectionProvider _protection = new();
    private Mock<IWebHostEnvironment> _environment = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ncf-workspace-assets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _environment = new Mock<IWebHostEnvironment>();
        _environment.SetupGet(value => value.ContentRootPath).Returns(_directory);
        _environment.SetupGet(value => value.WebRootPath).Returns(Path.Combine(_directory, "wwwroot"));
        _environment.SetupGet(value => value.EnvironmentName).Returns("Development");
        _storage = new Mock<IWorkspaceStorage>();
        _storage.Setup(value => value.GetAsync("ws_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceDescriptor("ws_test", 7, "Test", "/not-exposed", DateTime.UtcNow, DateTime.UtcNow));
        _storage.Setup(value => value.ReadFileBytesAsync("ws_test", It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string path, int limit, CancellationToken _) =>
            {
                if (!_files.TryGetValue(path, out var content)) throw new FileNotFoundException("Missing file.");
                if (content.Length > limit) throw new InvalidOperationException("File too large.");
                return new WorkspaceFileContent(Metadata(path, false, content.Length), content.ToArray());
            });
        _repository = new Mock<IAdminChatSessionRepository>();
        var session = new AdminChatSession("Test", 7, "ws_test");
        typeof(AdminChatSession).GetProperty("Id")!.SetValue(session, 42);
        _repository.Setup(value => value.GetFirstOrDefaultObjectAsync(
                It.IsAny<Expression<Func<AdminChatSession, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<AdminChatSession, bool>> predicate, string[] _) =>
                predicate.Compile()(session) ? session : null);
        _services = new ServiceCollection().AddSingleton(_storage.Object)
            .AddSingleton<IOptions<WorkspaceOptions>>(Options.Create(new WorkspaceOptions())).BuildServiceProvider();
        var sessions = new AdminChatSessionService(_repository.Object, _services);
        _assets = new AdminChatWorkspaceAssetService(sessions, _services, new WorkspaceAssetPreviewBuilder());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _services.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    [TestMethod]
    public async Task Preview_EmbedsLocalCssImagesFonts_AndStripsActiveContent()
    {
        Put("site/index.html", """
            <script>window.unsafe=1</script><base href="https://outside.invalid/">
            <meta http-equiv="refresh" content="0;url=https://outside.invalid">
            <link rel="stylesheet" href="css/site.css">
            <img src="images/logo.png" onerror="window.unsafe=1">
            <a href="https://outside.invalid/" ping="/api/action">Navigate</a>
            <form action="/api/action"><button>Submit</button></form>
            <style>.inline{background:url(images/logo.png)}</style>
            """);
        Put("site/css/site.css", "@import 'base.css'; .logo{background:url('../images/logo.png')} @font-face{src:url('font.woff2')}");
        Put("site/css/base.css", "body{color:navy}");
        _files["site/css/font.woff2"] = [1, 2, 3];
        _files["site/images/logo.png"] = [4, 5, 6];
        var result = await _assets.BuildPreviewAsync("ws_test", "site/index.html", default);
        var document = new HtmlParser().ParseDocument(result.Html);
        Assert.AreEqual(0, document.QuerySelectorAll("script, base, form, [onerror], [ping]").Length);
        Assert.AreEqual(1, document.QuerySelectorAll("meta[http-equiv]").Length);
        Assert.IsNull(document.QuerySelector("a")!.GetAttribute("href"));
        StringAssert.StartsWith(document.QuerySelector("img")!.GetAttribute("src")!, "data:image/png;base64,");
        var css = Encoding.UTF8.GetString(Convert.FromBase64String(
            document.QuerySelector("link")!.GetAttribute("href")!.Split(',')[1]));
        StringAssert.Contains(css, "data:font/woff2;base64,");
        StringAssert.Contains(css, "data:image/png;base64,");
        StringAssert.Contains(css, "data:text/css;base64,");
        Assert.IsTrue(result.Warnings.Length > 0);
    }

    [TestMethod]
    public async Task Preview_BlocksExternalResources_AndRejectsOutOfScopeOrMissingResources()
    {
        Put("site/index.html", "<img src='https://outside.invalid/track'><h1>Safe</h1>");
        var result = await _assets.BuildPreviewAsync("ws_test", "site/index.html", default);
        Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("外部资源")));
        Assert.IsFalse(result.Html.Contains("outside.invalid", StringComparison.Ordinal));
        Put("site/index.html", "<img src='../secrets.png'>");
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _assets.BuildPreviewAsync("ws_test", "site/index.html", default));
        Put("site/index.html", "<img src='missing.png'>");
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => _assets.BuildPreviewAsync("ws_test", "site/index.html", default));
        _storage.Verify(value => value.ReadFileBytesAsync("ws_test", "secrets.png", It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Put("site #1/index.html", "<img src='logo%20one.png'>");
        _files["site #1/logo one.png"] = [1, 2, 3];
        var encodedPathPreview = await _assets.BuildPreviewAsync("ws_test", "site #1/index.html", default);
        StringAssert.Contains(encodedPathPreview.Html, "data:image/png;base64,");
    }

    [TestMethod]
    public async Task Access_RejectsAnonymousNonAdminOtherOwnerAndOtherSession()
    {
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            _assets.GetSessionAsync(42, new ClaimsPrincipal(), default));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            _assets.GetSessionAsync(42, User(7, "reader"), default));
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(() =>
            _assets.GetSessionAsync(42, User(8), default));
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(() =>
            _assets.GetSessionAsync(43, User(7), default));
        Assert.AreEqual("ws_test", (await _assets.GetSessionAsync(42, User(7))).WorkspaceId);
        _storage.Setup(value => value.GetAsync("ws_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceDescriptor("ws_test", 8, "Other", "", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            _assets.GetSessionAsync(42, User(7), default));
    }

    [TestMethod]
    public async Task Download_PreservesBinaryContent_AndZipsNestedAndEmptyDirectories()
    {
        _files["site/logo.png"] = [0, 255, 1, 0];
        Put("site/docs/guide.md", "# Guide");
        _storage.Setup(value => value.GetFileInfoAsync("ws_test", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string path, CancellationToken _) => Metadata(path, path == "site", 0));
        _storage.Setup(value => value.ListAsync("ws_test", "site", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Entry("site/logo.png"), Entry("site/docs", true), Entry("site/empty", true)]);
        _storage.Setup(value => value.ListAsync("ws_test", "site/docs", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Entry("site/docs/guide.md")]);
        _storage.Setup(value => value.ListAsync("ws_test", "site/empty", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var file = await _assets.DownloadAsync("ws_test", "site/logo.png", default);
        CollectionAssert.AreEqual(_files["site/logo.png"], file.Bytes);
        var result = await _assets.DownloadAsync("ws_test", "site", default);
        Assert.AreEqual("site.zip", result.FileName);
        using var archive = new ZipArchive(new MemoryStream(result.Bytes), ZipArchiveMode.Read);
        Assert.IsNotNull(archive.GetEntry("empty/"));
        using var text = new StreamReader(archive.GetEntry("docs/guide.md")!.Open());
        Assert.AreEqual("# Guide", await text.ReadToEndAsync());
        using var image = new MemoryStream();
        await archive.GetEntry("logo.png")!.Open().CopyToAsync(image);
        CollectionAssert.AreEqual(_files["site/logo.png"], image.ToArray());
    }

    [TestMethod]
    public async Task Download_RejectsUnsafeArchivePathsAndTooManyEntries()
    {
        _storage.Setup(value => value.GetFileInfoAsync("ws_test", "site", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Metadata("site", true, 0));
        _storage.Setup(value => value.ListAsync("ws_test", "site", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Entry("../secret")]);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _assets.DownloadAsync("ws_test", "site", default));
        _storage.Setup(value => value.ListAsync("ws_test", "site", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, 1001).Select(index => Entry($"site/folder{index}", true)).ToArray());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _assets.DownloadAsync("ws_test", "site", default));
    }

    [TestMethod]
    public async Task PreviewAndArchive_RejectOversizedFilesAndCssCycles()
    {
        _files["index.html"] = new byte[WorkspaceAssetPreviewBuilder.MaxFileBytes + 1];
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _assets.BuildPreviewAsync("ws_test", "index.html", default));
        Put("index.html", "<link rel='stylesheet' href='site.css'>");
        Put("site.css", "@import 'site.css';");
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _assets.BuildPreviewAsync("ws_test", "index.html", default));
        _storage.Setup(value => value.GetFileInfoAsync("ws_test", "site", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Metadata("site", true, 0));
        _storage.Setup(value => value.ListAsync("ws_test", "site", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new WorkspaceEntry("site/large.bin", false, 100 * 1024 * 1024, DateTime.UtcNow)]);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _assets.DownloadAsync("ws_test", "site", default));
    }

    [TestMethod]
    public async Task Shares_AreEncryptedPersistentScopedAndRevocable()
    {
        WorkspaceAssetShare share;
        using (var store = Store())
            share = await store.CreateAsync(7, 42, "ws_test", "index.html", "<h1>snapshot-secret</h1>", 24);
        var bytes = await File.ReadAllBytesAsync(Directory.GetFiles(_directory, "*.protected", SearchOption.AllDirectories).Single());
        Assert.IsFalse(Encoding.UTF8.GetString(bytes).Contains("snapshot-secret", StringComparison.Ordinal));
        Assert.IsFalse(Encoding.UTF8.GetString(bytes).Contains(share.Token, StringComparison.Ordinal));
        using var reopened = Store();
        Assert.AreEqual(share.Html, (await reopened.ReadPublicAsync(share.Id, share.Token))!.Html);
        Assert.IsNull(await reopened.ReadPublicAsync(share.Id, new string('a', 43)));
        Assert.IsNull(await reopened.ReadPublicAsync("../invalid", share.Token));
        Assert.AreEqual(0, (await reopened.ListAsync(8, 42, "ws_test")).Count);
        Assert.IsFalse(await reopened.RevokeAsync(8, 42, "ws_test", share.Id));
        Assert.IsFalse(await reopened.RevokeAsync(7, 43, "ws_test", share.Id));
        Assert.IsTrue(await reopened.RevokeAsync(7, 42, "ws_test", share.Id));
        Assert.IsNull(await reopened.ReadPublicAsync(share.Id, share.Token));
    }

    [TestMethod]
    public async Task Shares_ExpireExactlyAndLimitLifetimeQuotaAndPublicRequests()
    {
        using var store = Store();
        var share = await store.CreateAsync(7, 42, "ws_test", "index.html", "snapshot", 1);
        _clock.Now = share.ExpiresAt;
        Assert.IsNull(await store.ReadPublicAsync(share.Id, share.Token));
        Assert.AreEqual(0, Directory.GetFiles(_directory, "*.protected", SearchOption.AllDirectories).Length);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            store.CreateAsync(7, 42, "ws_test", "index.html", "snapshot", 999));
        for (var index = 0; index < 10; index++)
            await store.CreateAsync(7, 42, "ws_test", "index.html", "snapshot", 24);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            store.CreateAsync(7, 42, "ws_test", "index.html", "snapshot", 24));
        for (var index = 0; index < 30; index++) Assert.IsTrue(store.TryAcquirePublicRequest("127.0.0.1"));
        Assert.IsFalse(store.TryAcquirePublicRequest("127.0.0.1"));
    }

    [TestMethod]
    public async Task Shares_RequireHttpsOutsideLocalDevelopment_AndStorageOutsideWebRoot()
    {
        using var store = Store();
        var controller = new AdminChatWorkspaceAssetsController(_assets, store,
            NullLogger<AdminChatWorkspaceAssetsController>.Instance, _environment.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = User(7) } }
        };
        controller.Request.Host = new HostString("example.invalid");
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Shared("id", "token", default));
        controller.Request.Host = new HostString("localhost");
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        _environment.SetupGet(value => value.EnvironmentName).Returns("Production");
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Shared("id", "token", default));
        controller.Request.Scheme = "https";
        Assert.IsInstanceOfType<NotFoundObjectResult>(await controller.Shared("id", "token", default));
        _environment.SetupGet(value => value.WebRootPath)
            .Returns(Path.Combine(_directory, "App_Data", "WorkspaceAssetShares"));
        Assert.ThrowsException<InvalidOperationException>(() => Store());
    }

    [TestMethod]
    public async Task Controller_ExternalPreviewHasSandboxAndAnonymousSurfaceIsOnlySnapshotGet()
    {
        Put("index.html", "<h1>Safe preview</h1>");
        using var store = Store();
        var controller = new AdminChatWorkspaceAssetsController(_assets, store,
            NullLogger<AdminChatWorkspaceAssetsController>.Instance, _environment.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = User(7) } }
        };
        var result = await controller.Preview(42, "index.html", default);
        Assert.IsInstanceOfType<FileContentResult>(result);
        var headers = controller.Response.Headers;
        StringAssert.Contains(headers["Content-Security-Policy"].ToString(), "sandbox;");
        Assert.IsFalse(headers["Content-Security-Policy"].ToString().Contains("allow-same-origin", StringComparison.Ordinal));
        Assert.IsFalse(headers["Content-Security-Policy"].ToString().Contains("allow-scripts", StringComparison.Ordinal));
        Assert.AreEqual("no-store", headers.CacheControl.ToString());
        Assert.AreEqual("no-referrer", headers["Referrer-Policy"].ToString());
        var anonymous = typeof(AdminChatWorkspaceAssetsController).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length > 0).ToArray();
        Assert.AreEqual(1, anonymous.Length);
        Assert.AreEqual("Shared", anonymous[0].Name);
        controller.HttpContext.User = User(8);
        Assert.IsInstanceOfType<NotFoundObjectResult>(await controller.Preview(42, "index.html", default));
    }

    [TestMethod]
    public async Task HttpBoundary_RequiresLoginAndAntiforgery_ButAllowsOnlyValidAnonymousSnapshot()
    {
        Put("index.html", "<h1>HTTP snapshot</h1>");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(_assets);
        builder.Services.AddSingleton(Store());
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AdminChatWorkspaceAssetsController).Assembly);
        builder.Services.AddAntiforgery();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(SiteConfig.NcfAdminAuthorizeScheme)
            .AddScheme<AuthenticationSchemeOptions, FixtureAuthenticationHandler>(SiteConfig.NcfAdminAuthorizeScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, FixtureAuthenticationHandler>(AdminOrJwtAuthorizeAttribute.JwtScheme, _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy(NcfAuthorizationPolicyNames.AdminOnly,
            policy => policy.RequireAuthenticatedUser()));
        await using var application = builder.Build();
        application.UseDeveloperExceptionPage();
        application.UseAuthentication();
        application.UseAuthorization();
        application.MapControllers();
        application.MapGet("/fixture/token", (HttpContext context, IAntiforgery antiforgery) =>
            new { token = antiforgery.GetAndStoreTokens(context).RequestToken });
        await application.StartAsync();
        try
        {
            var address = application.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };
            const string root = "/api/Senparc.Areas.Admin/WorkspaceAssets";
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(root + "/42/preview?path=index.html")).StatusCode);
            client.DefaultRequestHeaders.Add("X-Fixture-User", "7");
            using var authenticatedPreview = await client.GetAsync(root + "/42/preview?path=index.html");
            Assert.AreEqual(HttpStatusCode.OK, authenticatedPreview.StatusCode, await authenticatedPreview.Content.ReadAsStringAsync());
            using var missingCsrf = await client.PostAsync(root + "/42/shares",
                new StringContent("{\"path\":\"index.html\",\"expiresInHours\":1}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
            using var tokenJson = JsonDocument.Parse(await client.GetStringAsync("/fixture/token"));
            client.DefaultRequestHeaders.Add("RequestVerificationToken", tokenJson.RootElement.GetProperty("token").GetString());
            using var created = await client.PostAsync(root + "/42/shares",
                new StringContent("{\"path\":\"index.html\",\"expiresInHours\":1}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.OK, created.StatusCode);
            using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var share = createdJson.RootElement.GetProperty("share");
            var publicUrl = share.GetProperty("url").GetString()!;
            var id = share.GetProperty("id").GetString();
            using var anonymous = new HttpClient { BaseAddress = client.BaseAddress };
            var preview = await anonymous.GetAsync(publicUrl);
            Assert.AreEqual(HttpStatusCode.OK, preview.StatusCode);
            Assert.AreEqual("text/html", preview.Content.Headers.ContentType!.MediaType);
            StringAssert.Contains(preview.Headers.GetValues("Content-Security-Policy").Single(), "sandbox;");
            Assert.AreEqual("no-store", preview.Headers.CacheControl!.ToString());
            Put("index.html", "<h1>Changed live file</h1>");
            StringAssert.Contains(await anonymous.GetStringAsync(publicUrl), "HTTP snapshot");
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(root + "/42/download?path=index.html")).StatusCode);
            using var revoked = await client.PostAsync(root + $"/42/shares/{id}/revoke", null);
            Assert.AreEqual(HttpStatusCode.OK, revoked.StatusCode);
            Assert.AreEqual(HttpStatusCode.NotFound, (await anonymous.GetAsync(publicUrl)).StatusCode);
        }
        finally { await application.StopAsync(); }
    }

    private WorkspaceAssetShareStore Store() => new(_environment.Object, _protection,
        NullLogger<WorkspaceAssetShareStore>.Instance, _clock);
    private void Put(string path, string content) => _files[path] = Encoding.UTF8.GetBytes(content);
    private static WorkspaceEntry Entry(string path, bool directory = false) => new(path, directory, 0, DateTime.UtcNow);
    private static WorkspaceFileMetadata Metadata(string path, bool directory, int size) =>
        new(path, directory, size, DateTime.UtcNow, DateTime.UtcNow, "etag");
    private static ClaimsPrincipal User(int id, string role = "administrator") => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role)], "test"));
    private sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    public sealed class FixtureAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public FixtureAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(Request.Headers["X-Fixture-User"] == "7"
                ? AuthenticateResult.Success(new AuthenticationTicket(User(7), Scheme.Name))
                : AuthenticateResult.NoResult());
    }
}
