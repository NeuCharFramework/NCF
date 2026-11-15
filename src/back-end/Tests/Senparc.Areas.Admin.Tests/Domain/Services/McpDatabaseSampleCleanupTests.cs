using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using ModelContextProtocol.Server;
using Senparc.Xncf.MCP.Models;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using Senparc.Xncf.MCP.OHS.Local.AppService;
using Senparc.Xncf.MCP.OHS.Local.PL;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
[DoNotParallelize]
public class McpDatabaseSampleCleanupTests
{
    private const string ColorTable = "Senparc_MCP_Color";
    private const string EndpointTable = "Senparc_MCP_MCPEndpoint";
    private const string EndpointEntity = "Senparc.Xncf.MCP.Models.DatabaseModel.MCPEndpoint";

    [DataTestMethod]
    [DataRow("Sqlite")]
    [DataRow("SqlServer")]
    [DataRow("PostgreSQL")]
    [DataRow("MySql")]
    [DataRow("Oracle")]
    public void CleanupMigration_DropsOnlyColorAndRetainsHistoricalEndpointModel(string provider)
    {
        using var context = CreateContext(provider);
        var assembly = context.GetService<IMigrationsAssembly>();
        var migrations = assembly.Migrations.OrderBy(x => x.Key).ToArray();
        Assert.AreEqual(3, migrations.Length);
        Assert.IsTrue(migrations[0].Key.EndsWith("_Init", StringComparison.Ordinal));
        Assert.IsTrue(migrations[1].Key.EndsWith("_Add_MCPEndpoint", StringComparison.Ordinal));
        Assert.IsTrue(migrations[2].Key.EndsWith("_Remove_DatabaseSample", StringComparison.Ordinal));

        var cleanup = assembly.CreateMigration(migrations[2].Value, context.Database.ProviderName!);
        var previous = assembly.CreateMigration(migrations[1].Value, context.Database.ProviderName!);
        Assert.AreEqual(1, cleanup.UpOperations.Count);
        var drop = cleanup.UpOperations.Single() as DropTableOperation;
        Assert.IsNotNull(drop);
        Assert.AreEqual(ColorTable, drop.Name);
        Assert.AreEqual(1, cleanup.DownOperations.Count);
        var restore = cleanup.DownOperations.Single() as CreateTableOperation;
        Assert.IsNotNull(restore);
        Assert.AreEqual(ColorTable, restore.Name);

        var initial = assembly.CreateMigration(migrations[0].Value, context.Database.ProviderName!);
        var originalColor = initial.UpOperations.OfType<CreateTableOperation>().Single();
        CollectionAssert.AreEquivalent(
            originalColor.Columns.Select(ColumnSignature).ToArray(),
            restore.Columns.Select(ColumnSignature).ToArray());

        var snapshot = assembly.ModelSnapshot!.Model;
        Assert.AreEqual(1, snapshot.GetEntityTypes().Count());
        Assert.AreEqual(1, cleanup.TargetModel.GetEntityTypes().Count());
        Assert.IsNull(snapshot.FindEntityType("Senparc.Xncf.MCP.Color"));
        CollectionAssert.AreEqual(
            PropertySignatures(previous.TargetModel.FindEntityType(EndpointEntity)!),
            PropertySignatures(cleanup.TargetModel.FindEntityType(EndpointEntity)!));
        CollectionAssert.AreEqual(
            PropertySignatures(previous.TargetModel.FindEntityType(EndpointEntity)!),
            PropertySignatures(snapshot.FindEntityType(EndpointEntity)!));

        var migrator = context.GetService<IMigrator>();
        var sql = migrator.GenerateScript(migrations[1].Key, migrations[2].Key);
        StringAssert.Contains(sql, ColorTable);
        StringAssert.Contains(sql.ToUpperInvariant(), "DROP TABLE");
        Assert.IsFalse(sql.Contains(EndpointTable, StringComparison.OrdinalIgnoreCase), sql);
        var rollbackSql = migrator.GenerateScript(migrations[2].Key, migrations[1].Key);
        StringAssert.Contains(rollbackSql, ColorTable);
        Assert.IsFalse(rollbackSql.Contains(EndpointTable, StringComparison.OrdinalIgnoreCase), rollbackSql);
    }

    [TestMethod]
    public async Task SqliteUpgradeAndRollback_PreserveEveryEndpointColumnAcrossTenants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new MCPSenparcEntities_Sqlite(
            new DbContextOptionsBuilder<MCPSenparcEntities_Sqlite>().UseSqlite(connection).Options);
        var migrator = context.GetService<IMigrator>();
        var endpointMigration = context.Database.GetMigrations().Single(x => x.EndsWith("_Add_MCPEndpoint"));
        await migrator.MigrateAsync(endpointMigration);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Senparc_MCP_Color"
                ("Red", "Green", "Blue", "Flag", "TenantId", "AddTime", "LastUpdateTime")
                VALUES (10, 20, 30, 0, 1, '2026-10-01', '2026-10-02');
            CREATE TABLE "UnrelatedTable" ("Value" TEXT NOT NULL);
            INSERT INTO "UnrelatedTable" VALUES ('preserve me');
            """);
        for (var tenant = 1; tenant <= 2; tenant++)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Senparc_MCP_MCPEndpoint"
                    ("Name", "Endpoint", "EndpointType", "ProtocolVersion", "Description",
                     "Enabled", "AuthConfig", "ExtraConfig", "LastTestedTime", "LastTestResult",
                     "LastToolsJson", "LastToolCount", "Flag", "AddTime", "LastUpdateTime",
                     "TenantId", "AdminRemark", "Remark")
                VALUES
                    ({"Endpoint " + tenant}, {"https://example.test/mcp?tenant=" + tenant}, {"sse"},
                     {"2025-06-18"}, {"配置说明 " + tenant}, {tenant == 1},
                     {"{\"headers\":{\"Authorization\":\"test-only-value\"}}"}, {"{\"custom\":true}"},
                     {"2026-10-02 12:34:56.123"}, {tenant == 1},
                     {"[{\"name\":\"tool\",\"description\":\"" + new string('x', 2500) + "\"}]"},
                     {3}, {tenant == 2}, {"2026-10-01 01:02:03.456"},
                     {"2026-10-02 02:03:04.567"}, {tenant}, {"admin note"}, {"remark"});
                """);
        }
        var before = await ReadEndpointRows(connection);
        Assert.AreEqual(2, before.Length);
        await migrator.MigrateAsync();
        Assert.AreEqual(0, await TableCount(connection, ColorTable));
        CollectionAssert.AreEqual(before, await ReadEndpointRows(connection));
        Assert.AreEqual(1, await TableCount(connection, "UnrelatedTable"));
        Assert.AreEqual(0, (await context.Database.GetPendingMigrationsAsync()).Count());

        await migrator.MigrateAsync(endpointMigration);
        Assert.AreEqual(1, await TableCount(connection, ColorTable));
        CollectionAssert.AreEqual(before, await ReadEndpointRows(connection));
        await migrator.MigrateAsync();
        Assert.AreEqual(0, await TableCount(connection, ColorTable));
        CollectionAssert.AreEqual(before, await ReadEndpointRows(connection));
    }

    [TestMethod]
    public async Task SqliteFreshInstallAndLegacyUpgrade_EndWithEndpointTableOnly()
    {
        foreach (var upgradeLegacy in new[] { false, true })
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            await using var context = new MCPSenparcEntities_Sqlite(
                new DbContextOptionsBuilder<MCPSenparcEntities_Sqlite>().UseSqlite(connection).Options);
            if (upgradeLegacy)
            {
                await context.GetService<IMigrator>().MigrateAsync(context.Database.GetMigrations().First());
                Assert.AreEqual(1, await TableCount(connection, ColorTable));
                Assert.AreEqual(0, await TableCount(connection, EndpointTable));
            }
            await context.Database.MigrateAsync();
            Assert.AreEqual(0, await TableCount(connection, ColorTable));
            Assert.AreEqual(1, await TableCount(connection, EndpointTable));
        }
    }

    [TestMethod]
    public void RuntimeModule_RemovesSampleContractsAndKeepsMcpToolsAndCall()
    {
        var assembly = typeof(MCPSenparcEntities).Assembly;
        foreach (var type in new[]
        {
            "Senparc.Xncf.MCP.Color",
            "Senparc.Xncf.MCP.Models.DatabaseModel.Dto.ColorDto",
            "Senparc.Xncf.MCP.Domain.Services.ColorService",
            "Senparc.Xncf.MCP.OHS.Local.AppService.ColorAppService",
            "Senparc.Xncf.MCP.OHS.Local.PL.Color_GetOrInitColorResponse",
            "Senparc.Xncf.MCP.OHS.Local.PL.MyFunction_CaculateRequest",
            "Senparc.Xncf.MCP.Areas.MCP.Pages.DatabaseSample"
        })
        {
            Assert.IsNull(assembly.GetType(type), type);
        }
        Assert.AreEqual(typeof(DbSet<MCPEndpoint>),
            typeof(MCPSenparcEntities).GetProperty("MCPEndpoints")!.PropertyType);
        Assert.IsNull(typeof(MCPSenparcEntities).GetProperty("Colors"));
        Assert.AreEqual(typeof(IServiceProvider),
            typeof(MyFuctionAppService).GetConstructors().Single().GetParameters().Single().ParameterType);
        Assert.AreEqual(typeof(MyFunction_MCPCallRequest),
            typeof(MyFuctionAppService).GetMethod("GetMcpResult")!.GetParameters().Single().ParameterType);
        foreach (var tool in new[] { "Echo", "Now", "AddHours", "BobLab" })
        {
            Assert.IsNotNull(typeof(NcfMcpTools).GetMethod(tool)!.GetCustomAttribute<McpServerToolAttribute>());
        }
        Assert.IsNotNull(typeof(MyFuctionAppService).GetMethod("Calculator")!
            .GetCustomAttribute<McpServerToolAttribute>());
    }

    private static string ColumnSignature(AddColumnOperation column) =>
        $"{column.Name}|{column.ColumnType}|{column.IsNullable}|{column.MaxLength}|" +
        string.Join(";", column.GetAnnotations().OrderBy(x => x.Name).Select(x => $"{x.Name}={x.Value}"));

    private static string[] PropertySignatures(IEntityType entity) => entity.GetProperties()
        .OrderBy(x => x.Name)
        .Select(x => $"{x.Name}|{x.ClrType}|{x.IsNullable}|{x.GetColumnType()}|{x.GetMaxLength()}|{x.ValueGenerated}|" +
            string.Join(";", x.GetAnnotations().OrderBy(a => a.Name).Select(a => $"{a.Name}={a.Value}")))
        .ToArray();

    private static DbContext CreateContext(string provider) => provider switch
    {
        "Sqlite" => new MCPSenparcEntities_Sqlite(new DbContextOptionsBuilder<MCPSenparcEntities_Sqlite>()
            .UseSqlite("Data Source=:memory:").Options),
        "SqlServer" => new MCPSenparcEntities_SqlServer(new DbContextOptionsBuilder<MCPSenparcEntities_SqlServer>()
            .UseSqlServer("Server=localhost;Database=McpCleanup;Integrated Security=true").Options),
        "PostgreSQL" => new MCPSenparcEntities_PostgreSQL(new DbContextOptionsBuilder<MCPSenparcEntities_PostgreSQL>()
            .UseNpgsql("Host=localhost;Database=McpCleanup;Username=mcp;Password=unused").Options),
        "MySql" => new MCPSenparcEntities_MySql(new DbContextOptionsBuilder<MCPSenparcEntities_MySql>()
            .UseMySql("Server=localhost;Database=McpCleanup;User=mcp;Password=unused",
                new MySqlServerVersion(new Version(8, 0, 0))).Options),
        "Oracle" => new MCPSenparcEntities_Oracle(new DbContextOptionsBuilder<MCPSenparcEntities_Oracle>()
            .UseOracle("Data Source=localhost;User Id=mcp;Password=unused").Options),
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    private static async Task<int> TableCount(SqliteConnection connection, string name)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        command.Parameters.AddWithValue("$name", name);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<string[]> ReadEndpointRows(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM \"{EndpointTable}\" ORDER BY \"Id\"";
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(JsonSerializer.Serialize(values));
        }
        return rows.ToArray();
    }
}
