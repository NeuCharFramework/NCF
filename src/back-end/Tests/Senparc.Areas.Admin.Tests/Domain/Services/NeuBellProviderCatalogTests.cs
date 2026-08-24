/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellProviderCatalogTests.cs
    文件功能描述：Admin Footer 纽铃 Provider 启用状态筛选测试

    创建标识：Senparc - 20260802
----------------------------------------------------------------*/

using Microsoft.Extensions.Logging.Abstractions;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuBellProviderCatalogTests
{
    [TestMethod]
    public async Task GetAvailableProvidersAsync_ShouldReturnOnlyOpenModules()
    {
        var openProvider = new TestProvider("open", "module-open");
        var closedProvider = new TestProvider("closed", "module-closed");
        var legacyProvider = new LegacyProvider();
        var catalog = CreateCatalog(
            new INeuBellProvider[] { closedProvider, legacyProvider, openProvider },
            new TestAvailabilityService("module-open"));

        var result = await catalog.GetAvailableProvidersAsync();

        Assert.AreEqual(1, result.Count);
        Assert.AreSame(openProvider, result[0]);
    }

    [TestMethod]
    public async Task GetAvailableProvidersAsync_StateFailure_ShouldFailClosed()
    {
        var catalog = CreateCatalog(
            new[] { new TestProvider("provider", "module-open") },
            new FailingAvailabilityService());

        var result = await catalog.GetAvailableProvidersAsync();

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetSnapshotsAsync_ClosedModule_ShouldNotInvokeProvider()
    {
        var provider = new TestProvider("closed", "module-closed");
        var catalog = CreateCatalog(
            new[] { provider },
            new TestAvailabilityService());
        var snapshotService = new NeuBellSnapshotService(
            catalog,
            new NeuBellChangeNotifier(),
            NullLogger<NeuBellSnapshotService>.Instance);

        var snapshots = await snapshotService.GetSnapshotsAsync(new NeuBellRequestContext("admin"));

        Assert.AreEqual(0, snapshots.Count);
        Assert.AreEqual(0, provider.InvocationCount);
    }

    private static NeuBellProviderCatalog CreateCatalog(
        IEnumerable<INeuBellProvider> providers,
        INeuBellModuleAvailabilityService availabilityService)
    {
        return new NeuBellProviderCatalog(
            providers,
            availabilityService,
            NullLogger<NeuBellProviderCatalog>.Instance);
    }

    private sealed class TestAvailabilityService : INeuBellModuleAvailabilityService
    {
        private readonly IReadOnlySet<string> _openModuleUids;

        public TestAvailabilityService(params string[] openModuleUids)
        {
            _openModuleUids = openModuleUids.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public Task<IReadOnlySet<string>> GetOpenModuleUidsAsync(
            IEnumerable<string> moduleUids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_openModuleUids);
        }
    }

    private sealed class FailingAvailabilityService : INeuBellModuleAvailabilityService
    {
        public Task<IReadOnlySet<string>> GetOpenModuleUidsAsync(
            IEnumerable<string> moduleUids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlySet<string>>(new InvalidOperationException("database unavailable"));
        }
    }

    private sealed class TestProvider : INeuBellProvider
    {
        public TestProvider(string providerId, string moduleUid)
        {
            ProviderId = providerId;
            ModuleUid = moduleUid;
        }

        public string ProviderId { get; }

        public string ModuleUid { get; }

        public int InvocationCount { get; private set; }

        public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return ValueTask.FromResult(new NeuBellSnapshot(
                ProviderId,
                ModuleUid,
                ProviderId,
                "fa fa-test",
                true,
                Array.Empty<NeuBellItem>()));
        }
    }

    private sealed class LegacyProvider : INeuBellProvider
    {
        public string ProviderId => "legacy";

        public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Legacy provider must not be invoked without ModuleUid.");
        }
    }
}
