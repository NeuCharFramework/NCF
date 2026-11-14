using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using Senparc.Xncf.NeuCharWorkflow.ACL;
using Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using Senparc.Xncf.Tenant.ACL.Repository;
using Senparc.Xncf.Tenant.Domain.Cache;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Models;
using WorkflowEntity = Senparc.Xncf.NeuCharWorkflow.Domain.Models.DatabaseModel.NeuCharWorkflow;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class RepositoryAccessTests
{
    [TestInitialize]
    public void Initialize()
    {
        EntitySetKeys.TryLoadSetInfo(typeof(RepositoryTestContext));
    }

    [TestMethod]
    public async Task PollingAccounts_UsesRepositoryTenantAndSoftDeleteFilters()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var firstTenant = CreateContext(databaseName, 11);
        var enabled = CreateAccount("enabled");
        var disabled = CreateAccount("disabled");
        disabled.UpdateSettings(disabled.Name, disabled.BaseUrl, null, null, false);
        var deleted = CreateAccount("deleted");
        deleted.Flag = true;
        firstTenant.AddRange(enabled, disabled, deleted, CreateAccount("no-token", " "));
        await firstTenant.SaveChangesAsync();

        await using var secondTenant = CreateContext(databaseName, 22);
        secondTenant.Add(CreateAccount("other-tenant"));
        await secondTenant.SaveChangesAsync();

        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateAccountService(firstTenant, provider);
        var accounts = await service.GetPollingAccountsAsync();
        Assert.AreEqual(1, accounts.Count);
        Assert.AreEqual((11, enabled.Id), accounts.Single());

        var otherAccounts = await CreateAccountService(secondTenant, provider).GetPollingAccountsAsync();
        Assert.AreEqual(1, otherAccounts.Count);
        Assert.AreEqual(22, otherAccounts.Single().TenantId);
    }

    [TestMethod]
    public async Task PollingAccounts_SingleTenantModeStillExcludesSoftDeletedAccounts()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using var context = new SingleTenantRepositoryTestContext(options);
        var first = CreateAccount("first");
        first.TenantId = 11;
        var second = CreateAccount("second");
        second.TenantId = 22;
        var deleted = CreateAccount("deleted");
        deleted.Flag = true;
        context.AddRange(first, second, deleted);
        await context.SaveChangesAsync();

        using var provider = new ServiceCollection().BuildServiceProvider();
        var accounts = await CreateAccountService(context, provider).GetPollingAccountsAsync();
        CollectionAssert.AreEqual(new[] { first.Id, second.Id }, accounts.Select(z => z.AccountId).ToArray());
    }

    [TestMethod]
    public async Task WorkflowProjections_PreserveTenantFiltersPagingAndCancellation()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName, 11);
        var workflow = new WorkflowEntity("visible", 7);
        var deletedWorkflow = new WorkflowEntity("deleted", 7) { Flag = true };
        var older = new NeuCharWorkflowExecutionLog(123, "visible", "older");
        older.Complete(true, "ok", null, "older-events");
        var newer = new NeuCharWorkflowExecutionLog(123, "visible", "newer");
        newer.Complete(true, "ok", null, "newer-events");
        var deletedLog = new NeuCharWorkflowExecutionLog(123, "deleted", "deleted") { Flag = true };
        deletedLog.Complete(true, "ok", null, "deleted-events");
        context.AddRange(workflow, deletedWorkflow, older, newer, deletedLog);
        await context.SaveChangesAsync();

        await using var otherTenant = CreateContext(databaseName, 22);
        otherTenant.Add(new WorkflowEntity("other-tenant", 7));
        otherTenant.Add(new NeuCharWorkflowExecutionLog(123, "other-tenant", "other"));
        await otherTenant.SaveChangesAsync();

        using var provider = new ServiceCollection().BuildServiceProvider();
        var data = new TestDbData(context);
        var workflowService = new NeuCharWorkflowService(new NeuCharWorkflowRepository(data), provider);
        var logService = new NeuCharWorkflowExecutionLogService(new NeuCharWorkflowExecutionLogRepository(data), provider);
        var names = await workflowService.GetNameMapAsync(7);
        Assert.AreEqual(1, names.Count);
        Assert.AreEqual("visible", names[workflow.Id]);

        var page = await logService.GetTaskPageAsync([123], null, 1, "success");
        Assert.AreEqual(newer.Id, page.Single().Id);
        var nextPage = await logService.GetTaskPageAsync([123], newer.Id, 1, "success");
        Assert.AreEqual(older.Id, nextPage.Single().Id);
        var summary = await logService.GetTaskSummaryAsync([123]);
        Assert.AreEqual(2, summary.Count);
        var analytics = await logService.GetAnalyticsLogsAsync([123], "success");
        Assert.AreEqual(2, analytics.Count);
        var replayEvents = await logService.GetRecentCompletedReplayEventsAsync(123, 1);
        Assert.AreEqual("newer-events", replayEvents.Single());
        Assert.AreEqual(0, await logService.GetUnfinishedCountAsync(123));
        Assert.IsNull(await logService.GetUnfinishedByIdAsync(deletedLog.Id));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => workflowService.GetNameMapAsync(7, cancellation.Token));
    }

    [TestMethod]
    public async Task PartialRepositorySave_RejectsInvalidPropertiesBeforeChangingTracking()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"), 11);
        var workflow = new WorkflowEntity("visible", 7);
        context.Add(workflow);
        await context.SaveChangesAsync();
        workflow.MarkStarted(null);
        var repository = new NeuCharWorkflowRepository(new TestDbData(context));

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => repository.SavePropertiesAsync(workflow, "MissingProperty"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => repository.SavePropertiesAsync(workflow, nameof(workflow.Id)));
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => repository.SavePropertiesAsync(workflow));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => repository.SavePropertiesAsync(new WorkflowEntity("detached", 7), nameof(workflow.LastRunAt)));

        Assert.IsTrue(context.Entry(workflow).Property(z => z.LastRunAt).IsModified);
    }

    [TestMethod]
    public async Task TenantRegistryRepository_UsesIndependentGlobalContextAndSoftDeleteFilter()
    {
        Senparc.Ncf.Core.Register.TryRegisterMiniCore();
        EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntitiesMultiTenant));
        using var provider = new ServiceCollection().BuildServiceProvider();
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using var context = new SenparcEntitiesMultiTenant(options, provider);
        context.SetMultiTenantEnable(true);
        context.TenantInfo = new RequestTenantInfo { Id = 11 };
        var first = new TenantInfo("first", true, "FIRST");
        var second = new TenantInfo("second", true, "SECOND");
        var disabled = new TenantInfo("disabled", false, "DISABLED");
        var deleted = new TenantInfo("deleted", true, "DELETED") { Flag = true };
        context.AddRange(first, second, disabled, deleted);
        await context.SaveChangesAsync();
        var data = new TenantInfoDbData(context);
        var repository = new TenantInfoRepository(data);

        var tenants = await repository.GetObjectListAsync(
            z => z.Enable, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending, 0, 0);
        CollectionAssert.AreEqual(new[] { first.Id, second.Id }, tenants.Select(z => z.Id).ToArray());
        Assert.AreSame(context, repository.BaseDB.BaseDataContext);

        var services = new ServiceCollection();
        services.AddSingleton(data);
        services.AddSingleton(context);
        var mapper = new AutoMapper.MapperConfiguration(configuration =>
            configuration.AddProfile<Senparc.Xncf.Tenant.Domain.DatabaseModel.AutoMapper.TenantInfoProfile>()).CreateMapper();
        services.AddSingleton(mapper);
        services.AddSingleton(repository);
        services.AddScoped<FullTenantInfoCache>();
        using var cacheProvider = services.BuildServiceProvider();
        using var scope = cacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<FullTenantInfoCache>();
        var cachedTenants = await cache.UpdateAsync();
        CollectionAssert.AreEquivalent(new[] { "FIRST", "SECOND" }, cachedTenants.Keys.ToArray());
        var backgroundProvider = new Senparc.Xncf.Tenant.Domain.Services.BackgroundTenantProvider(cache);
        var enabledTenants = await backgroundProvider.GetEnabledTenantsAsync(CancellationToken.None);
        CollectionAssert.AreEquivalent(new[] { first.Id, second.Id }, enabledTenants.Select(z => z.Id).ToArray());
        Assert.IsTrue(enabledTenants.All(z => z.MatchSuccess && !string.IsNullOrEmpty(z.TenantKey)));
        await cache.RemoveCacheAsync();
    }

    private static WeixinClawAccount CreateAccount(string name, string token = "protected-token") =>
        new(name, "https://example.test", token, null);

    private static WeixinClawAccountService CreateAccountService(DbContext context, IServiceProvider provider) =>
        new(new RepositoryBase<WeixinClawAccount>(new TestDbData(context)), provider,
            new EphemeralDataProtectionProvider());

    private static RepositoryTestContext CreateContext(string databaseName, int tenantId)
    {
        var options = new DbContextOptionsBuilder().UseInMemoryDatabase(databaseName).Options;
        return new RepositoryTestContext(options, tenantId);
    }

    private class RepositoryTestContext : SenparcEntitiesDbContextBase
    {
        public DbSet<WeixinClawAccount> Accounts => Set<WeixinClawAccount>();
        public DbSet<WorkflowEntity> Workflows => Set<WorkflowEntity>();
        public DbSet<NeuCharWorkflowExecutionLog> Logs => Set<NeuCharWorkflowExecutionLog>();

        public RepositoryTestContext(DbContextOptions options, int tenantId) : base(options, null!)
        {
            SetMultiTenantEnable(true);
            TenantInfo = new RequestTenantInfo { Id = tenantId };
            TenantInfo.TryMatch(true);
        }
    }

    private sealed class SingleTenantRepositoryTestContext : RepositoryTestContext
    {
        public SingleTenantRepositoryTestContext(DbContextOptions options) : base(options, 0)
        {
            SetMultiTenantEnable(false);
        }
    }

    private sealed class TestDbData(DbContext context) : NcfDbData
    {
        public override DbContext BaseDataContext => context;
        public override void CloseConnection() { }
    }
}
