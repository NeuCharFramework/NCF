using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using AdminMigrations = Senparc.Areas.Admin.Domain.Migrations;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatSessionOrderTests
{
    [TestMethod]
    public async Task Reorder_PersistsFullGroupWithoutChangingActivity_AndSurvivesNewContext()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection, 11);
        await db.Database.EnsureCreatedAsync();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateService(db, provider);
        var first = await service.CreateSessionAsync("first", 12, "alpha");
        var second = await service.CreateSessionAsync("second", 12, "alpha");
        var third = await service.CreateSessionAsync("third", 12, "alpha");
        first.SetSortOrder(0);
        second.SetSortOrder(1);
        third.SetSortOrder(2);
        var unrelated = await service.CreateSessionAsync("unrelated", 12, "beta");
        await db.SaveChangesAsync();
        var lastActivity = first.LastMessageTime;
        var updateTime = first.LastUpdateTime;

        var reordered = await service.ReorderSessionAsync(12, first.Id, third.Id, true);
        CollectionAssert.AreEqual(new[] { second.Id, third.Id, first.Id }, reordered.Select(s => s.Id).ToArray());
        CollectionAssert.AreEqual(new int?[] { 0, 1, 2 }, reordered.Select(s => s.SortOrder).ToArray());
        Assert.AreEqual(lastActivity, first.LastMessageTime);
        Assert.AreEqual(updateTime, first.LastUpdateTime);
        Assert.IsNull(unrelated.SortOrder);
        Assert.AreEqual(2, AdminChatSessionDto.CreateFromEntity(first).SortOrder);

        await using var reopened = CreateContext(connection, 11);
        var stored = await reopened.Sessions.Where(s => s.WorkspaceId == "alpha").OrderBy(s => s.SortOrder).ToListAsync();
        CollectionAssert.AreEqual(reordered.Select(s => s.Id).ToArray(), stored.Select(s => s.Id).ToArray());
        var newService = CreateService(reopened, provider);
        var before = await newService.ReorderSessionAsync(12, first.Id, second.Id, false);
        CollectionAssert.AreEqual(new[] { first.Id, second.Id, third.Id }, before.Select(s => s.Id).ToArray());
    }

    [TestMethod]
    public async Task Reorder_RejectsOtherUsersTenantsGroupsAndInactiveSessions_WithoutPartialWrites()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection, 11);
        await db.Database.EnsureCreatedAsync();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateService(db, provider);
        var source = await service.CreateSessionAsync("source", 12, "alpha");
        var target = await service.CreateSessionAsync("target", 12, "alpha");
        var otherUser = await service.CreateSessionAsync("other-user", 13, "alpha");
        var otherGroup = await service.CreateSessionAsync("other-group", 12, "beta");
        var archived = await service.CreateSessionAsync("archived", 12, "alpha");
        var deleted = await service.CreateSessionAsync("deleted", 12, "alpha");
        var flagged = await service.CreateSessionAsync("flagged", 12, "alpha");
        archived.Archive();
        deleted.Delete();
        flagged.Flag = true;
        await db.SaveChangesAsync();
        await using var tenant = CreateContext(connection, 22);
        var otherTenant = await CreateService(tenant, provider).CreateSessionAsync("other-tenant", 12, "alpha");

        foreach (var targetId in new[] { source.Id, otherUser.Id, otherGroup.Id, archived.Id, deleted.Id, flagged.Id, otherTenant.Id, int.MaxValue })
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.ReorderSessionAsync(12, source.Id, targetId, true));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.ReorderSessionAsync(0, source.Id, target.Id, false));
        Assert.IsFalse(await db.Sessions.AnyAsync(s => s.SortOrder != null));
    }

    [TestMethod]
    public async Task Reorder_SupportsSessionsWithoutWorkspace_AndKeepsNewSessionsAfterManualOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection, 11);
        await db.Database.EnsureCreatedAsync();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = CreateService(db, provider);
        var first = await service.CreateSessionAsync("first", 12);
        var second = await service.CreateSessionAsync("second", 12);
        await service.ReorderSessionAsync(12, first.Id, second.Id, false);
        var newest = await service.CreateSessionAsync("newest", 12);
        Assert.IsNull(newest.SortOrder);
        var reordered = await service.ReorderSessionAsync(12, newest.Id, second.Id, false);
        CollectionAssert.AreEqual(new[] { first.Id, newest.Id, second.Id }, reordered.Select(s => s.Id).ToArray());
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => first.SetSortOrder(-1));
    }

    [TestMethod]
    public async Task Migrations_AddOnlyNullableRank_AndSqliteUpgradePreservesExistingRows()
    {
        Migration[] migrations =
        [
            new AdminMigrations.Sqlite.Add_AdminChat_Session_SortOrder(),
            new AdminMigrations.SqlServer.Add_AdminChat_Session_SortOrder(),
            new AdminMigrations.MySql.Add_AdminChat_Session_SortOrder(),
            new AdminMigrations.PostgreSQL.Add_AdminChat_Session_SortOrder(),
            new AdminMigrations.Oracle.Add_AdminChat_Session_SortOrder(),
            new AdminMigrations.Dm.Add_AdminChat_Session_SortOrder()
        ];
        foreach (var migration in migrations)
        {
            var column = (AddColumnOperation)migration.UpOperations.Single();
            Assert.AreEqual("ADMIN_AdminChatSession", column.Table);
            Assert.AreEqual("SortOrder", column.Name);
            Assert.AreEqual(typeof(int), column.ClrType);
            Assert.IsTrue(column.IsNullable);
            Assert.IsNull(column.DefaultValue);
            Assert.AreEqual("SortOrder", ((DropColumnOperation)migration.DownOperations.Single()).Name);
        }
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection, 11);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE ADMIN_AdminChatSession (Id INTEGER PRIMARY KEY, Title TEXT);");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO ADMIN_AdminChatSession (Id, Title) VALUES (7, 'existing');");
        var commands = db.GetService<IMigrationsSqlGenerator>().Generate(migrations[0].UpOperations, db.Model);
        foreach (var command in commands) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await using var read = connection.CreateCommand();
        read.CommandText = "SELECT Title, SortOrder FROM ADMIN_AdminChatSession WHERE Id = 7";
        await using var reader = await read.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual("existing", reader.GetString(0));
        Assert.IsTrue(reader.IsDBNull(1));
    }

    private static SessionContext CreateContext(SqliteConnection connection, int tenantId)
    {
        EntitySetKeys.TryLoadSetInfo(typeof(SessionContext));
        return new SessionContext(new DbContextOptionsBuilder().UseSqlite(connection).Options, tenantId);
    }

    private static AdminChatSessionService CreateService(SessionContext db, IServiceProvider provider) =>
        new(new AdminChatSessionRepository(new SessionDbData(db)), provider);

    private sealed class SessionContext : SenparcEntitiesDbContextBase
    {
        public DbSet<AdminChatSession> Sessions => Set<AdminChatSession>();

        public SessionContext(DbContextOptions options, int tenantId) : base(options, null!)
        {
            SetMultiTenantEnable(true);
            TenantInfo = new RequestTenantInfo { Id = tenantId };
            TenantInfo.TryMatch(true);
        }
    }

    private sealed class SessionDbData(DbContext context) : NcfDbData
    {
        public override DbContext BaseDataContext => context;
        public override void CloseConnection() { }
    }
}
