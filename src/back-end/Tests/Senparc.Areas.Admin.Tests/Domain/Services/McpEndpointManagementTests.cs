using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.ApiBind;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using Senparc.Ncf.XncfBase.MCP;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using Senparc.Xncf.MCP.OHS.Local.AppService;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
[DoNotParallelize]
public class McpEndpointManagementTests
{
    private const string Endpoint = "http://localhost:5080/mcp-senparc-xncf-mcp/sse";
    private SqliteConnection _connection = null!;
    private EndpointTestContext _context = null!;
    private ServiceProvider _provider = null!;
    private MCPEndpointAppService _appService = null!;
    private int _originalLogCacheMinutes;

    [TestInitialize]
    public async Task Initialize()
    {
        Senparc.Ncf.Core.Register.TryRegisterMiniCore();
        _originalLogCacheMinutes = SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes;
        SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes = 0;
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _context = new EndpointTestContext(new DbContextOptionsBuilder().UseSqlite(_connection).Options);
        EntitySetKeys.TryLoadSetInfo(typeof(EndpointTestContext));
        await _context.Database.EnsureCreatedAsync();
        _provider = new ServiceCollection()
            .AddHttpContextAccessor()
            .AddSingleton<McpConnectionTestService>()
            .AddSingleton(new McpServerInfoCollection())
            .AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource(Array.Empty<Endpoint>()))
            .AddSingleton<PublishedMcpServerService>()
            .BuildServiceProvider();
        var service = new MCPEndpointService(
            new RepositoryBase<MCPEndpoint>(new TestDbData(_context)), _provider);
        _appService = new MCPEndpointAppService(_provider, service);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
        await _provider.DisposeAsync();
        SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes = _originalLogCacheMinutes;
    }

    [TestMethod]
    public async Task SqliteLifecycle_CreatesListsEditsTogglesAndDeletesReportedSseUrl()
    {
        var created = await _appService.SaveEndpoint(CreateRequest());
        Assert.IsTrue(created.Success);
        _context.ChangeTracker.Clear();
        var list = await _appService.GetAllEndpoints();
        Assert.IsTrue(list.Success);
        var row = list.Data.Single();
        Assert.AreEqual(Endpoint, row.Endpoint);
        Assert.AreEqual("sse", row.EndpointType);
        Assert.IsTrue(row.Enabled);

        var edit = CreateRequest();
        edit.Id = row.Id;
        edit.Name = "Edited";
        edit.Enabled = false;
        edit.Description = "Edited description";
        edit.ExtraConfig = "{\"custom\":true}";
        Assert.IsTrue((await _appService.SaveEndpoint(edit)).Success);
        _context.ChangeTracker.Clear();
        Assert.AreEqual(0, (await _appService.GetEnabledEndpoints()).Data.Count);
        var updated = (await _appService.GetAllEndpoints()).Data.Single();
        Assert.AreEqual("Edited description", updated.Description);
        Assert.AreEqual(edit.ExtraConfig, updated.ExtraConfig);
        Assert.IsFalse(updated.Enabled);

        edit.Enabled = true;
        Assert.IsTrue((await _appService.SaveEndpoint(edit)).Success);
        Assert.AreEqual(1, (await _appService.GetEnabledEndpoints()).Data.Count);
        Assert.IsTrue((await _appService.DeleteEndpoint(new() { Id = row.Id })).Success);
        Assert.AreEqual(0, (await _appService.GetAllEndpoints()).Data.Count);
    }

    [TestMethod]
    public async Task Save_PreservesCustomPathsAndTrimsOnlySurroundingWhitespace()
    {
        var request = CreateRequest();
        request.Name = " Local MCP ";
        request.Endpoint = " https://example.test/custom/events?tenant=1 ";
        Assert.IsTrue((await _appService.SaveEndpoint(request)).Success);
        var row = (await _appService.GetAllEndpoints()).Data.Single();
        Assert.AreEqual("Local MCP", row.Name);
        Assert.AreEqual("https://example.test/custom/events?tenant=1", row.Endpoint);
    }

    [DataTestMethod]
    [DataRow("", Endpoint, 0)]
    [DataRow("  ", Endpoint, 0)]
    [DataRow("Local MCP", "", 0)]
    [DataRow("Local MCP", "  ", 0)]
    [DataRow("Local MCP", Endpoint, -1)]
    [DataRow("Local MCP", Endpoint, 999)]
    public async Task InvalidSave_ReturnsFailureInsteadOfSuccess(string name, string endpoint, int id)
    {
        var request = CreateRequest();
        request.Name = name;
        request.Endpoint = endpoint;
        request.Id = id;
        var response = await _appService.SaveEndpoint(request);
        Assert.IsFalse(response.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.ErrorMessage));
        Assert.AreEqual(0, await _context.Endpoints.CountAsync());
    }

    [TestMethod]
    public async Task DuplicateNames_AreRejectedOnBothCreateAndEdit()
    {
        Assert.IsTrue((await _appService.SaveEndpoint(CreateRequest())).Success);
        Assert.IsFalse((await _appService.SaveEndpoint(CreateRequest())).Success);
        var second = CreateRequest();
        second.Name = "Second";
        Assert.IsTrue((await _appService.SaveEndpoint(second)).Success);
        second.Id = (await _appService.GetAllEndpoints()).Data.Single(z => z.Name == "Second").Id;
        second.Name = "Local MCP";
        Assert.IsFalse((await _appService.SaveEndpoint(second)).Success);
        _context.ChangeTracker.Clear();
        Assert.AreEqual(2, await _context.Endpoints.CountAsync());
        Assert.AreEqual("Second", (await _appService.GetAllEndpoints()).Data.Single(z => z.Id == second.Id).Name);
    }

    [TestMethod]
    public async Task OverlengthInput_IsRejectedBeforePersistence()
    {
        var request = CreateRequest();
        request.Name = new string('x', 101);
        Assert.IsFalse((await _appService.SaveEndpoint(request)).Success);
        request.Name = "Local MCP";
        request.Endpoint = new string('x', 501);
        Assert.IsFalse((await _appService.SaveEndpoint(request)).Success);
        Assert.AreEqual(0, await _context.Endpoints.CountAsync());
    }

    [TestMethod]
    public async Task EditingConnectionSettings_ClearsStaleTestHistoryButTogglingDoesNot()
    {
        Assert.IsTrue((await _appService.SaveEndpoint(CreateRequest())).Success);
        var entity = await _context.Endpoints.SingleAsync();
        entity.LastTestedTime = DateTime.UtcNow;
        entity.LastTestResult = true;
        entity.LastToolCount = 1;
        entity.LastToolsJson = "[{\"Name\":\"Echo\"}]";
        await _context.SaveChangesAsync();
        var request = CreateRequest();
        request.Id = entity.Id;
        request.Enabled = false;
        Assert.IsTrue((await _appService.SaveEndpoint(request)).Success);
        Assert.IsTrue(entity.LastTestResult);
        request.Endpoint = "https://example.test/other/events";
        Assert.IsTrue((await _appService.SaveEndpoint(request)).Success);
        _context.ChangeTracker.Clear();
        var updated = await _context.Endpoints.SingleAsync();
        Assert.IsNull(updated.LastTestedTime);
        Assert.IsNull(updated.LastTestResult);
        Assert.IsNull(updated.LastToolCount);
        Assert.IsNull(updated.LastToolsJson);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(999)]
    public async Task InvalidDelete_ReturnsExplicitFailure(int id)
    {
        var response = await _appService.DeleteEndpoint(new() { Id = id });
        Assert.IsFalse(response.Success);
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.ErrorMessage));
    }

    [TestMethod]
    public async Task ConnectionFailure_PersistsTestHistoryWithoutReportingSuccess()
    {
        var request = CreateRequest();
        request.Endpoint = "not-a-url";
        Assert.IsTrue((await _appService.SaveEndpoint(request)).Success);
        var row = (await _appService.GetAllEndpoints()).Data.Single();
        var response = await _appService.TestEndpoint(new() { Id = row.Id });
        Assert.IsTrue(response.Success);
        Assert.IsFalse(response.Data.Success);
        Assert.AreEqual(400, response.Data.Status);
        _context.ChangeTracker.Clear();
        var tested = await _context.Endpoints.SingleAsync();
        Assert.IsNotNull(tested.LastTestedTime);
        Assert.IsFalse(tested.LastTestResult);
        Assert.AreEqual(0, tested.LastToolCount);
        Assert.IsNull(tested.LastToolsJson);
    }

    [TestMethod]
    public async Task PreSaveTest_RejectsUnsupportedTransportAndDoesNotPersist()
    {
        var response = await _appService.TestConnection(new()
        {
            Name = "Local MCP", Endpoint = Endpoint, EndpointType = "stdio"
        });
        Assert.IsTrue(response.Success);
        Assert.IsFalse(response.Data.Success);
        Assert.AreEqual(400, response.Data.Status);
        Assert.AreEqual(0, await _context.Endpoints.CountAsync());
    }

    [TestMethod]
    public async Task HttpConnection_DiscoversSchemasAndPersistsHistoryOnTheExactConfiguredPath()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        var requests = new List<(string Path, string Authorization)>();
        app.MapPost("/custom/mcp", async (HttpContext context) =>
        {
            requests.Add((context.Request.Path, context.Request.Headers.Authorization.ToString()));
            using var message = await JsonDocument.ParseAsync(context.Request.Body);
            if (!message.RootElement.TryGetProperty("id", out var id))
            {
                return Results.Accepted();
            }
            var method = message.RootElement.GetProperty("method").GetString();
            object result = method == "initialize"
                ? new
                {
                    protocolVersion = message.RootElement.GetProperty("params").GetProperty("protocolVersion").GetString(),
                    capabilities = new { tools = new { } },
                    serverInfo = new { name = "Regression MCP", version = "1.0" }
                }
                : new
                {
                    tools = new[] { new
                    {
                        name = "Echo", title = "Echo tool", description = "Returns a message",
                        inputSchema = new
                        {
                            type = "object", required = new[] { "message" },
                            properties = new { message = new { type = "string", description = "Message to echo" } }
                        }
                    } }
                };
            return Results.Json(new { jsonrpc = "2.0", id = id.Clone(), result });
        });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var request = CreateRequest();
        request.Endpoint = address + "/custom/mcp";
        request.EndpointType = "http";
        request.AuthConfig = "{\"token\":\"test-token\"}";
        Assert.IsTrue((await _appService.SaveEndpoint(request)).Success);
        var row = (await _appService.GetAllEndpoints()).Data.Single();
        var response = await _appService.TestEndpoint(new() { Id = row.Id });
        Assert.IsTrue(response.Success);
        Assert.IsTrue(response.Data.Success, response.Data.StatusMessage);
        Assert.AreEqual(1, response.Data.ToolCount);
        var tool = response.Data.Tools.Single();
        Assert.AreEqual("Echo", tool.Name);
        Assert.AreEqual("Message to echo", tool.Parameters.Single().Description);
        Assert.IsTrue(tool.Parameters.Single().Required);
        Assert.IsTrue(tool.InputSchemaJson!.Contains("\"type\":\"object\""));
        Assert.IsTrue(requests.All(z => z.Path == "/custom/mcp" && z.Authorization == "Bearer test-token"));
        _context.ChangeTracker.Clear();
        var tested = await _context.Endpoints.SingleAsync();
        Assert.IsTrue(tested.LastTestResult);
        Assert.AreEqual(1, tested.LastToolCount);
        Assert.IsTrue(tested.LastToolsJson!.Contains("Echo"));
        Assert.AreEqual(request.Endpoint, tested.Endpoint);
        await app.StopAsync();
    }

    [TestMethod]
    public async Task ConnectionTimeout_IsBoundedAndCallerCancellationIsNotReportedAsATimeout()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        app.MapPost("/slow", async (HttpContext context) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(20), context.RequestAborted);
        });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var tester = new McpConnectionTestService();
        var started = System.Diagnostics.Stopwatch.StartNew();
        var timeout = await tester.TestAsync("Slow MCP", address + "/slow", timeoutSeconds: 3, endpointType: "http");
        Assert.IsFalse(timeout.Success);
        Assert.AreEqual(408, timeout.Status, timeout.StatusMessage);
        Assert.IsTrue(started.Elapsed < TimeSpan.FromSeconds(6), $"Elapsed: {started.Elapsed}");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try
        {
            await tester.TestAsync("Slow MCP", address + "/slow", endpointType: "http", cancellationToken: cancellation.Token);
            Assert.Fail("Caller cancellation must propagate.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        await app.StopAsync();
    }

    [TestMethod]
    public async Task LiveSseConnection_WhenExplicitlyEnabled_DiscoversToolsAtReportedUrl()
    {
        var url = Environment.GetEnvironmentVariable("MCP_LIVE_TEST_URL");
        if (string.IsNullOrWhiteSpace(url))
        {
            Assert.Inconclusive("Set MCP_LIVE_TEST_URL to run the live MCP SSE regression.");
        }
        var tester = new McpConnectionTestService();
        var result = await tester.TestAsync("Live MCP", url, endpointType: "sse");
        Assert.IsTrue(result.Success, result.StatusMessage);
        Assert.IsTrue(result.ToolCount > 0);
    }

    [TestMethod]
    public async Task GeneratedApis_AreAuthenticatedPostRoutesAndExecuteAllSevenOperations()
    {
        var type = typeof(MCPEndpointAppService);
        Assert.IsNotNull(type.GetCustomAttribute<ApiAuthorizeAttribute>());
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.AreEqual(7, methods.Length);
        var bindings = new ApiBindInfoCollection();
        var category = type.Assembly.GetName().Name!;
        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<ApiBindAttribute>();
            Assert.IsNotNull(attribute, $"{method.Name} must generate a Web API.");
            Assert.AreEqual(ApiRequestMethod.Post, attribute.ApiRequestMethod);
            bindings.Add(ApiBindOn.Method, category, method, attribute);
        }
        var group = bindings.GroupBy(z => z.Value.Category).Single();
        var engine = new WebApiEngine(options => { options.CopyCustomAttributes = true; });
        WebApiEngine.ApiAssemblyNames[group.Key] = "McpEndpointRegression_" + Guid.NewGuid().ToString("N");
        Assert.AreEqual(7, await engine.BuildWebApi(group));
        var assembly = engine.GetApiAssembly(group.Key);
        var controller = assembly.GetTypes().Single(z => typeof(ControllerBase).IsAssignableFrom(z));
        const string path = "/api/Senparc.Xncf.MCP/MCPEndpointAppService/Xncf.MCP_MCPEndpointAppService.";
        foreach (var method in methods)
        {
            var action = controller.GetMethods().Single(z =>
                z.GetCustomAttribute<RouteAttribute>()?.Template?.TrimStart('/') ==
                (path + method.Name).TrimStart('/'));
            Assert.IsNotNull(action.GetCustomAttribute<HttpPostAttribute>());
            Assert.IsNotNull(action.GetCustomAttribute<ApiAuthorizeAttribute>());
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(_provider.GetRequiredService<IHttpContextAccessor>());
        builder.Services.AddSingleton(_appService);
        builder.Services.AddControllers().AddApplicationPart(assembly);
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                AdminAuthorizeAttribute.AuthenticationScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                ApiAuthorizeAttribute.JwtBearerScheme, _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        using var unauthorized = await client.PostAsJsonAsync(path + "GetAllEndpoints", new { });
        Assert.AreEqual(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Authenticated", "true");
        using var wrongVerb = await client.GetAsync(path + "SaveEndpoint");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, wrongVerb.StatusCode);

        Assert.IsTrue((await Post<StringAppResponse>("SaveEndpoint", CreateRequest())).Success);
        var list = await Post<AppResponseBase<List<MCPEndpointDto>>>("GetAllEndpoints", new { });
        Assert.IsTrue(list.Success);
        Assert.AreEqual(Endpoint, list.Data.Single().Endpoint);
        var published = await Post<AppResponseBase<List<PublishedMcpServerDto>>>("GetPublishedServers", new { });
        Assert.IsTrue(published.Success);
        Assert.AreEqual(0, published.Data.Count);
        Assert.AreEqual(1, (await Post<AppResponseBase<List<MCPEndpointDto>>>("GetEnabledEndpoints", new { })).Data.Count);
        var preTest = await Post<AppResponseBase<McpConnectionTestResult>>("TestConnection",
            new { endpoint = "not-a-url", endpointType = "sse" });
        Assert.IsFalse(preTest.Data.Success);
        var test = await Post<AppResponseBase<McpConnectionTestResult>>("TestEndpoint", new { id = 999 });
        Assert.IsFalse(test.Data.Success);
        Assert.IsTrue((await Post<StringAppResponse>("DeleteEndpoint", new { id = list.Data.Single().Id })).Success);
        Assert.AreEqual(0, (await Post<AppResponseBase<List<MCPEndpointDto>>>("GetAllEndpoints", new { })).Data.Count);
        await app.StopAsync();

        async Task<T> Post<T>(string method, object request)
        {
            using var response = await client.PostAsJsonAsync(path + method, request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }

    private static MCPEndpointCreateOrEditRequest CreateRequest() => new()
    {
        Name = "Local MCP", Endpoint = Endpoint, EndpointType = "sse", Enabled = true
    };

    private sealed class EndpointTestContext : SenparcEntitiesDbContextBase
    {
        public DbSet<MCPEndpoint> Endpoints => Set<MCPEndpoint>();

        public EndpointTestContext(DbContextOptions options) : base(options, null!)
        {
            SetMultiTenantEnable(true);
            TenantInfo = new RequestTenantInfo { Id = 11 };
            TenantInfo.TryMatch(true);
        }
    }

    private sealed class TestDbData(DbContext context) : NcfDbData
    {
        public override DbContext BaseDataContext => context;
        public override void CloseConnection() { }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-Test-Authenticated"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "test-admin")], Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
