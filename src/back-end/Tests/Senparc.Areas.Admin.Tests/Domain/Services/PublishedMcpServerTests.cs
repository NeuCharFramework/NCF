using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Senparc.Ncf.XncfBase.MCP;
using Senparc.Xncf.MCP.Domain.Services;
using System.Net.Http.Json;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class PublishedMcpServerTests
{
    [TestMethod]
    public void ModuleRegistration_ResolvesPublicationServiceAndHttpContextAccessor()
    {
        var services = new ServiceCollection().AddLogging().AddRouting();
        new Senparc.Xncf.MCP.Register().AddXncfModule(
            services, new ConfigurationBuilder().Build(), Mock.Of<IHostEnvironment>());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<PublishedMcpServerService>());
        Assert.IsNotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
    }

    [TestMethod]
    public void Discovery_RequiresBothCompletedRegistrationAndAnActualMappedRoute()
    {
        var registrations = new McpServerInfoCollection([
            Server("Published", "mcp-published"),
            Server("RegisteredOnly", null),
            Server("Stale", "mcp-stale"),
            Server("WrongMethod", "mcp-wrong"),
            Server("PrefixOnly", "mcp-prefix")
        ]);
        var service = CreateService(registrations,
            Route("/mcp-published/sse", "GET"),
            Route("/mcp-published/message", "POST"),
            Route("/mcp-registeredonly/sse", "GET"),
            Route("/mcp-unregistered/sse", "GET"),
            Route("/mcp-wrong/sse", "POST"),
            Route("/mcp-prefix-other/sse", "GET"));

        var servers = service.GetPublishedServers(Request());
        Assert.AreEqual(1, servers.Count);
        var server = servers.Single();
        Assert.AreEqual("Published", server.ServerName);
        Assert.AreEqual("Senparc.Xncf.Published", server.XncfName);
        Assert.AreEqual("uid-Published", server.XncfUid);
        Assert.AreEqual("/mcp-published", server.Route);
        Assert.AreEqual("sse", server.Endpoints.Single().EndpointType);
        Assert.AreEqual("https://public.example.test/mcp-published/sse", server.Endpoints.Single().Endpoint);
    }

    [TestMethod]
    public void Discovery_ReflectsActualSupportedTransportsAndDeploymentPathBase()
    {
        var registrations = new McpServerInfoCollection([Server("Dual", "/mcp-dual/")]);
        var service = CreateService(registrations,
            Route("/mcp-dual/sse", "GET"),
            Route("/mcp-dual", "GET", "POST", "DELETE"));
        var request = Request();
        request.Host = new HostString("public.example.test", 8443);
        request.PathBase = "/ncf";

        var server = service.GetPublishedServers(request).Single();
        Assert.AreEqual("/ncf/mcp-dual", server.Route);
        CollectionAssert.AreEquivalent(new[] { "sse", "http" },
            server.Endpoints.Select(z => z.EndpointType).ToArray());
        Assert.AreEqual("https://public.example.test:8443/ncf/mcp-dual/sse",
            server.Endpoints.Single(z => z.EndpointType == "sse").Endpoint);
        Assert.AreEqual("https://public.example.test:8443/ncf/mcp-dual",
            server.Endpoints.Single(z => z.EndpointType == "http").Endpoint);
    }

    [TestMethod]
    public void Discovery_StreamableHttpDoesNotInventALegacySseEndpoint()
    {
        var service = CreateService(new McpServerInfoCollection([Server("Http", "mcp-http")]),
            Route("/mcp-http", "POST"));
        var server = service.GetPublishedServers(Request()).Single();
        Assert.AreEqual("http", server.Endpoints.Single().EndpointType);
        Assert.AreEqual("https://public.example.test/mcp-http", server.Endpoints.Single().Endpoint);
    }

    [TestMethod]
    public void Discovery_AllMethodRoutesAreValidAndDuplicateMethodRoutesDoNotDuplicateEndpoints()
    {
        var service = CreateService(new McpServerInfoCollection([Server("AllMethods", "mcp-all")]),
            Route("/mcp-all/sse"),
            Route("/mcp-all/sse", "GET"),
            Route("/mcp-all"),
            Route("/mcp-all", "POST"));
        Assert.AreEqual(2, service.GetPublishedServers(Request()).Single().Endpoints.Count);
    }

    [TestMethod]
    public void Discovery_IsDeterministicallyOrderedAndDoesNotModifyRegistrations()
    {
        var registrations = new McpServerInfoCollection([Server("Zeta", "mcp-z"), Server("Alpha", "mcp-a")]);
        var service = CreateService(registrations, Route("/mcp-z/sse", "GET"), Route("/mcp-a/sse", "GET"));
        CollectionAssert.AreEqual(new[] { "Alpha", "Zeta" },
            service.GetPublishedServers(Request()).Select(z => z.ServerName).ToArray());
        Assert.AreEqual("mcp-a", registrations["Alpha"].McpRoute);
        Assert.AreEqual(2, registrations.Count);
    }

    [TestMethod]
    public async Task Discovery_UsesTheRealHostEndpointDataSourceAfterRuntimeMapping()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddRouting();
        builder.Services.AddSingleton(new McpServerInfoCollection([
            Server("Runtime", "mcp-runtime"), Server("Unmapped", "mcp-unmapped")
        ]));
        builder.Services.AddSingleton<PublishedMcpServerService>();
        await using var app = builder.Build();
        app.MapGet("/mcp-runtime/sse", () => Results.Ok());
        app.MapPost("/mcp-runtime/message", () => Results.Ok());
        app.MapGet("/published", (HttpContext context, PublishedMcpServerService service) =>
            service.GetPublishedServers(context.Request));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient();
        var servers = await client.GetFromJsonAsync<List<PublishedMcpServerDto>>(address + "/published");
        Assert.IsNotNull(servers);
        Assert.AreEqual("Runtime", servers.Single().ServerName);
        Assert.AreEqual(address + "/mcp-runtime/sse", servers.Single().Endpoints.Single().Endpoint);
        await app.StopAsync();
    }

    private static HttpRequest Request()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("public.example.test");
        return context.Request;
    }

    private static McpServerInfo Server(string name, string? route) => new()
    {
        ServerName = name, XncfName = "Senparc.Xncf." + name, XncfUid = "uid-" + name, McpRoute = route
    };

    private static PublishedMcpServerService CreateService(McpServerInfoCollection registrations, params Endpoint[] endpoints) =>
        new(registrations, new DefaultEndpointDataSource(endpoints));

    private static RouteEndpoint Route(string path, params string[] methods) => new(
        _ => Task.CompletedTask, RoutePatternFactory.Parse(path), 0,
        new EndpointMetadataCollection(new HttpMethodMetadata(methods)), path);
}
