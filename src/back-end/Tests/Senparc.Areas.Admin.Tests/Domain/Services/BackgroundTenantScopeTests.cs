using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System.Collections;
using System.Net;
using System.Reflection;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
[DoNotParallelize]
public class BackgroundTenantScopeTests
{
    private bool _originalMultiTenantEnabled;
    private static ServiceProvider? _utilityProvider;

    [ClassInitialize]
    public static void InitializeUtilities(TestContext _)
    {
        if (DI.ServiceProvider == null)
        {
            var environment = new Moq.Mock<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
            environment.SetupGet(z => z.ContentRootPath).Returns(AppContext.BaseDirectory);
            environment.SetupGet(z => z.WebRootPath).Returns(AppContext.BaseDirectory);
            var services = new ServiceCollection();
            services.AddSingleton(environment.Object);
            services.AddHttpContextAccessor();
            _utilityProvider = services.BuildServiceProvider();
            DI.ServiceProvider = _utilityProvider;
        }
    }

    [ClassCleanup]
    public static void CleanupUtilities()
    {
        if (ReferenceEquals(DI.ServiceProvider, _utilityProvider))
        {
            DI.ServiceProvider = null!;
        }
        _utilityProvider?.Dispose();
    }

    [TestInitialize]
    public void Initialize()
    {
        _originalMultiTenantEnabled = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
        SiteConfig.SenparcCoreSetting.EnableMultiTenant = true;
        EntitySetKeys.TryLoadSetInfo(typeof(BackgroundDbContext));
    }

    [TestCleanup]
    public void Cleanup() => SiteConfig.SenparcCoreSetting.EnableMultiTenant = _originalMultiTenantEnabled;

    [TestMethod]
    public async Task EnabledScopes_InitializeBeforeResolvingServicesAndDisposeIndependently()
    {
        using var provider = CreateProvider(new TestTenantProvider { Tenants = [Tenant(22), Tenant(11)] });
        var factory = provider.GetRequiredService<IBackgroundTenantScopeFactory>();
        var probes = new List<ScopedProbe>();
        await factory.ForEachEnabledTenantAsync((services, _) =>
        {
            var probe = services.GetRequiredService<ScopedProbe>();
            probes.Add(probe);
            Assert.IsTrue(probe.Tenant.MatchSuccess);
            Assert.AreEqual($"tenant-{probe.Tenant.Id}", probe.Tenant.TenantKey);
            Assert.AreSame(probe.Tenant, services.GetRequiredService<BackgroundDbContext>().TenantInfo);
            return Task.CompletedTask;
        });

        CollectionAssert.AreEqual(new[] { 11, 22 }, probes.Select(z => z.Tenant.Id).ToArray());
        Assert.AreNotSame(probes[0].Tenant, probes[1].Tenant);
        Assert.IsTrue(probes.All(z => z.Disposed));
    }

    [TestMethod]
    public async Task RepositoryQueries_UseInitializedTenantWithoutServiceSetTenantInfo()
    {
        using var provider = CreateProvider(new TestTenantProvider { Tenants = [Tenant(11), Tenant(22)] });
        var factory = provider.GetRequiredService<IBackgroundTenantScopeFactory>();
        await factory.ForEachEnabledTenantAsync(async (services, _) =>
        {
            var context = services.GetRequiredService<BackgroundDbContext>();
            context.Add(new WeixinClawAccount("visible", "https://example.test", "token", null));
            context.Add(new WeixinClawAccount("deleted", "https://example.test", "token", null) { Flag = true });
            await context.SaveChangesAsync();
        });
        await factory.ForEachEnabledTenantAsync(async (services, cancellationToken) =>
        {
            var accounts = await services.GetRequiredService<WeixinClawAccountService>()
                .GetPollingAccountsAsync(cancellationToken);
            Assert.AreEqual(1, accounts.Count);
            Assert.AreEqual(services.GetRequiredService<RequestTenantInfo>().Id, accounts.Single().TenantId);
        });
    }

    [TestMethod]
    public async Task DisabledTenant_DoesNotCreateExecutionScope()
    {
        var registry = new TestTenantProvider { Tenants = [Tenant(11), Tenant(22)] };
        using var provider = CreateProvider(registry);
        var factory = provider.GetRequiredService<IBackgroundTenantScopeFactory>();
        using (var scope = await factory.TryCreateScopeAsync(22))
        {
            Assert.AreEqual(22, scope!.ServiceProvider.GetRequiredService<RequestTenantInfo>().Id);
        }
        registry.Tenants = [Tenant(11)];
        Assert.IsNull(await factory.TryCreateScopeAsync(22));
        Assert.IsNull(await factory.TryCreateScopeAsync(99));
    }

    [TestMethod]
    public async Task SingleTenantMode_DoesNotRequireTenantRegistry()
    {
        SiteConfig.SenparcCoreSetting.EnableMultiTenant = false;
        using var provider = CreateProvider(null);
        var factory = provider.GetRequiredService<IBackgroundTenantScopeFactory>();
        var calls = 0;
        await factory.ForEachEnabledTenantAsync((services, _) =>
        {
            calls++;
            var tenant = services.GetRequiredService<RequestTenantInfo>();
            Assert.AreEqual(0, tenant.Id);
            Assert.IsTrue(tenant.MatchSuccess);
            return Task.CompletedTask;
        });
        Assert.AreEqual(1, calls);
        using var scope = await factory.TryCreateScopeAsync(22);
        Assert.IsNotNull(scope);
        Assert.AreEqual(0, scope.ServiceProvider.GetRequiredService<RequestTenantInfo>().Id);
    }

    [TestMethod]
    public async Task MissingOrInvalidRegistry_FailsInsteadOfFallingBackToGlobalAccess()
    {
        using var missingProvider = CreateProvider(null);
        var missingFactory = missingProvider.GetRequiredService<IBackgroundTenantScopeFactory>();
        await Assert.ThrowsExceptionAsync<NcfTenantException>(
            () => missingFactory.ForEachEnabledTenantAsync((_, _) => Task.CompletedTask));

        var registry = new TestTenantProvider { Tenants = [Tenant(11), Tenant(11)] };
        using var provider = CreateProvider(registry);
        var factory = provider.GetRequiredService<IBackgroundTenantScopeFactory>();
        await Assert.ThrowsExceptionAsync<NcfTenantException>(() => factory.TryCreateScopeAsync(11));
        registry.Tenants = [new RequestTenantInfo { Id = 11 }];
        await Assert.ThrowsExceptionAsync<NcfTenantException>(() => factory.TryCreateScopeAsync(11));
    }

    [TestMethod]
    public async Task CancellationOrActionFailure_DisposesTheActiveScope()
    {
        using var provider = CreateProvider(new TestTenantProvider { Tenants = [Tenant(11)] });
        var factory = provider.GetRequiredService<IBackgroundTenantScopeFactory>();
        using var cancellation = new CancellationTokenSource();
        ScopedProbe? active = null;
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
            factory.ForEachEnabledTenantAsync((services, _) =>
            {
                active = services.GetRequiredService<ScopedProbe>();
                cancellation.Cancel();
                return Task.CompletedTask;
            }, cancellation.Token));
        Assert.IsTrue(active!.Disposed);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            factory.ForEachEnabledTenantAsync((services, _) =>
            {
                active = services.GetRequiredService<ScopedProbe>();
                throw new InvalidOperationException("business failure");
            }));
        Assert.IsTrue(active.Disposed);
    }

    [TestMethod]
    public async Task HostedPolling_TenantDisableCancelsAndRemovesRunningAccount()
    {
        var registry = new TestTenantProvider { Tenants = [Tenant(11)] };
        using var provider = CreateProvider(registry);
        var factory = provider.GetRequiredService<IBackgroundTenantScopeFactory>();
        await factory.ForEachEnabledTenantAsync(async (services, _) =>
        {
            var service = services.GetRequiredService<WeixinClawAccountService>();
            var context = services.GetRequiredService<BackgroundDbContext>();
            context.Add(new WeixinClawAccount("polling", "https://example.test", service.ProtectToken("token"), null));
            await context.SaveChangesAsync();
        });

        using var handler = new BlockingPollHandler();
        using var client = new HttpClient(handler);
        var api = new WeixinClawApi(client);
        using var executionProvider = CreateProvider(registry, provider.GetRequiredService<DatabaseIdentity>(), api);
        using var host = new WeixinClawHostedService(executionProvider.GetRequiredService<IServiceScopeFactory>(),
            executionProvider.GetRequiredService<IBackgroundTenantScopeFactory>(),
            Options.Create(new WeixinClawHostedServiceOptions()),
            NullLogger<WeixinClawHostedService>.Instance);
        using var stopping = new CancellationTokenSource();
        try
        {
            await ScanAsync(host, stopping.Token);
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var running = RunningAccounts(host);
            var state = running.Values.Cast<object>().Single();
            var accountTask = (Task)state.GetType().GetProperty("Task")!.GetValue(state)!;

            await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => ScanAsync(host, stopping.Token))));
            Assert.AreEqual(1, running.Count);
            registry.Tenants = [];
            await ScanAsync(host, stopping.Token);
            await accountTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(0, running.Count);
            Assert.IsTrue(handler.Canceled);
        }
        finally
        {
            stopping.Cancel();
            var tasks = RunningAccounts(host).Values.Cast<object>()
                .Select(z => (Task)z.GetType().GetProperty("Task")!.GetValue(z)!).ToArray();
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static Task ScanAsync(WeixinClawHostedService host, CancellationToken cancellationToken) =>
        (Task)typeof(WeixinClawHostedService).GetMethod("ScanAccountsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(host, [cancellationToken])!;

    private static IDictionary RunningAccounts(WeixinClawHostedService host) =>
        (IDictionary)typeof(WeixinClawHostedService)
            .GetField("_runningAccounts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)!;

    private static RequestTenantInfo Tenant(int id)
    {
        var tenant = new RequestTenantInfo { Id = id, Name = $"Tenant {id}", TenantKey = $"tenant-{id}" };
        tenant.TryMatch(true);
        return tenant;
    }

    private static ServiceProvider CreateProvider(TestTenantProvider? registry,
        DatabaseIdentity? database = null, WeixinClawApi? api = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<RequestTenantInfo>();
        services.AddScoped<ScopedProbe>();
        services.AddSingleton(database ?? new DatabaseIdentity());
        services.AddSingleton<IDataProtectionProvider>(registry?.Protector ?? new EphemeralDataProtectionProvider());
        services.AddScoped<BackgroundDbContext>(serviceProvider => new BackgroundDbContext(
            new DbContextOptionsBuilder().UseInMemoryDatabase(serviceProvider.GetRequiredService<DatabaseIdentity>().Name).Options,
            serviceProvider));
        services.AddScoped<INcfDbData, TestDbData>();
        services.AddScoped<IRepositoryBase<WeixinClawAccount>, RepositoryBase<WeixinClawAccount>>();
        services.AddScoped<WeixinClawAccountService>();
        services.AddSingleton<IBackgroundTenantScopeFactory, BackgroundTenantScopeFactory>();
        if (registry != null)
        {
            services.AddSingleton<IBackgroundTenantProvider>(registry);
        }
        if (api != null)
        {
            services.AddSingleton(api);
        }
        return services.BuildServiceProvider();
    }

    private sealed class DatabaseIdentity
    {
        public string Name { get; } = Guid.NewGuid().ToString("N");
    }

    private sealed class TestTenantProvider : IBackgroundTenantProvider
    {
        public IDataProtectionProvider Protector { get; } = new EphemeralDataProtectionProvider();
        public IReadOnlyList<RequestTenantInfo> Tenants { get; set; } = [];
        public Task<IReadOnlyList<RequestTenantInfo>> GetEnabledTenantsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Tenants);
        }
    }

    private sealed class ScopedProbe(RequestTenantInfo tenant) : IDisposable
    {
        public RequestTenantInfo Tenant { get; } = tenant;
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private sealed class BackgroundDbContext(DbContextOptions options, IServiceProvider serviceProvider)
        : SenparcEntitiesDbContextBase(options, serviceProvider)
    {
        public DbSet<WeixinClawAccount> Accounts => Set<WeixinClawAccount>();
    }

    private sealed class TestDbData(BackgroundDbContext context) : NcfDbData
    {
        public override DbContext BaseDataContext => context;
        public override void CloseConnection() { }
    }

    private sealed class BlockingPollHandler : HttpMessageHandler
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Canceled { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/getupdates", StringComparison.Ordinal))
            {
                Started.TrySetResult(true);
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Canceled = true;
                    throw;
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ret\":0}") };
        }
    }
}
