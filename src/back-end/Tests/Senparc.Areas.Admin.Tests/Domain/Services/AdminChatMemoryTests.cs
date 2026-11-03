using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.Sqlite;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Abstractions.Memory;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatMemoryTests
{
    private string _root = null!;
    private IWebHostEnvironment _environment = null!;
    private IDataProtectionProvider _protection = null!;
    private AdminChatMemoryStore _store = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "ncf-memory-test-" + Guid.NewGuid().ToString("N"));
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ContentRootPath).Returns(_root);
        environment.SetupGet(value => value.WebRootPath).Returns(Path.Combine(_root, "wwwroot"));
        _environment = environment.Object;
        _protection = new EphemeralDataProtectionProvider();
        _store = new AdminChatMemoryStore(_environment, _protection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public async Task Store_DefaultReadDoesNotRequireWritableStorage()
    {
        var profile = await _store.ReadAsync(0, 12);
        Assert.AreEqual("", profile.Revision);
        Assert.AreEqual(0, profile.Entries.Count);
        Assert.IsFalse(Directory.Exists(_root));
    }

    [TestMethod]
    public async Task Store_IsolatesUsersTenantsAndGlobalSettings_AndEncryptsAtRest()
    {
        var saved = await _store.UpdateAsync(1, 12, "", profile => profile.SystemMessage = "private-working-preference");
        Assert.AreEqual("private-working-preference", (await _store.ReadAsync(1, 12)).SystemMessage);
        Assert.AreEqual("", (await _store.ReadAsync(1, 13)).SystemMessage);
        Assert.AreEqual("", (await _store.ReadAsync(2, 12)).SystemMessage);
        Assert.AreEqual("", (await _store.ReadAsync(1, 0)).SystemMessage);
        Assert.AreNotEqual("", saved.Revision);
        var encrypted = await File.ReadAllTextAsync(Path.Combine(_root, "App_Data", "AdminChatMemory", "1", "12.protected"));
        Assert.IsFalse(encrypted.Contains("private-working-preference"));
        var reopened = new AdminChatMemoryStore(_environment, _protection);
        Assert.AreEqual(saved.Revision, (await reopened.ReadAsync(1, 12)).Revision);
    }

    [TestMethod]
    public async Task Store_ConcurrentInstances_OnlyOneRevisionCanWin()
    {
        var second = new AdminChatMemoryStore(_environment, _protection);
        async Task<bool> Save(AdminChatMemoryStore store, string value)
        {
            try
            {
                await store.UpdateAsync(1, 12, "", profile => profile.SystemMessage = value);
                return true;
            }
            catch (NcfExceptionBase) { return false; }
        }
        var results = await Task.WhenAll(Save(_store, "one"), Save(second, "two"));
        Assert.AreEqual(1, results.Count(result => result));
        var actual = (await _store.ReadAsync(1, 12)).SystemMessage;
        Assert.IsTrue(actual is "one" or "two");
        Assert.AreEqual(0, Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories).Length);
    }

    [TestMethod]
    public async Task Store_RejectsStaleEditAndCancelledWrite()
    {
        var first = await _store.UpdateAsync(1, 12, "", profile => profile.SystemMessage = "manual");
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() =>
            _store.UpdateAsync(1, 12, "", profile => profile.SystemMessage = "stale"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
            _store.UpdateAsync(1, 12, first.Revision, profile => profile.SystemMessage = "cancelled", cancellation.Token));
        Assert.AreEqual("manual", (await _store.ReadAsync(1, 12)).SystemMessage);
    }

    [TestMethod]
    public void Store_RejectsPublicStorage()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ContentRootPath).Returns(_root);
        environment.SetupGet(value => value.WebRootPath).Returns(_root);
        Assert.ThrowsException<InvalidOperationException>(() => new AdminChatMemoryStore(environment.Object, _protection));
    }

    [TestMethod]
    public void Markdown_RoundTripsTiers_PinsManualEntries_AndRejectsInvalidFormat()
    {
        const string markdown = "# Memory\n\n## Core\n- Answer concisely\n\n## Topic: C#\n- Prefer async I/O\n\n## Archive\n- Old preference\n";
        var entries = AdminChatMemoryDocument.Parse(markdown);
        Assert.AreEqual(3, entries.Count);
        Assert.IsTrue(entries.All(entry => entry.Pinned));
        Assert.AreEqual(markdown, AdminChatMemoryDocument.Export(entries).Replace("\r\n", "\n"));
        Assert.AreEqual(0, AdminChatMemoryDocument.Parse("").Count);
        Assert.ThrowsException<NcfExceptionBase>(() => AdminChatMemoryDocument.Parse("free-form instructions"));
        Assert.ThrowsException<NcfExceptionBase>(() => AdminChatMemoryDocument.Parse("- " + new string('x', 301)));
        Assert.ThrowsException<NcfExceptionBase>(() => AdminChatMemoryDocument.Parse(string.Concat(Enumerable.Repeat("- x\n", 81))));
    }

    [TestMethod]
    public void Context_LoadsCoreAndRelevantTopics_NotArchives_WithinExactByteBudget()
    {
        var entries = AdminChatMemoryDocument.Parse(
            "## Core\n- 简洁回答\n## Topic: 数据库\n- 使用异步查询\n## Topic: UI\n- Use blue buttons\n## Archive\n- Never load this archive");
        var context = AdminChatMemoryDocument.BuildContext(entries, "请优化数据库查询");
        var decoded = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(context)!;
        Assert.AreEqual(2, decoded.Count);
        Assert.IsTrue(decoded.Any(item => item["Text"] == "使用异步查询"));
        Assert.IsFalse(context.Contains("blue") || context.Contains("archive"));
        var symbolTopic = AdminChatMemoryDocument.Parse("## Topic: C#\n- Prefer explicit types\n");
        StringAssert.Contains(AdminChatMemoryDocument.BuildContext(symbolTopic, "C#?"), "Prefer explicit types");
        var longEntries = Enumerable.Range(0, 80)
            .Select(index => new AdminChatMemoryEntry("key-" + index, new string('字', 150), "", "core", false, DateTimeOffset.UtcNow));
        var bounded = AdminChatMemoryDocument.BuildContext(longEntries, "");
        Assert.IsTrue(bounded.Length > 0);
        Assert.IsTrue(Encoding.UTF8.GetByteCount(bounded) <= AdminChatMemoryDocument.ContextByteBudget);
    }

    [TestMethod]
    public void Context_ReservesRoomForRelevantTopics_WhenCoreIsFull()
    {
        var entries = Enumerable.Range(0, 20)
            .Select(index => new AdminChatMemoryEntry("key-" + index, new string('x', 180), "", "core", false, DateTimeOffset.UtcNow)).ToList();
        entries.Add(new("topic", "Prefer typed APIs", "typescript", "topic", false, DateTimeOffset.UtcNow));
        var context = AdminChatMemoryDocument.BuildContext(entries, "typescript");
        StringAssert.Contains(context, "Prefer typed APIs");
        Assert.IsTrue(Encoding.UTF8.GetByteCount(context) <= AdminChatMemoryDocument.ContextByteBudget);
    }

    private static string Changes(string text = "Prefer concise replies", string evidence = "Prefer concise replies",
        string key = "response-style", bool remove = false) =>
        JsonSerializer.Serialize(new[] { new { key, text, topic = "", tier = "core", evidence, remove } });

    [TestMethod]
    public void Merge_RequiresVerbatimEvidence_ValidatesEntireBatchBeforeMutation()
    {
        var profile = new AdminChatMemoryProfile();
        Assert.ThrowsException<AdminChatMemoryValidationException>(() => AdminChatMemoryDocument.Merge(profile, Changes(), "hello"));
        Assert.AreEqual(0, profile.Entries.Count);
        Assert.IsTrue(AdminChatMemoryDocument.Merge(profile, Changes(), "Prefer concise replies"));
        Assert.IsFalse(AdminChatMemoryDocument.Merge(profile, Changes(), "Prefer concise replies"));
        Assert.ThrowsException<AdminChatMemoryValidationException>(() => AdminChatMemoryDocument.Merge(profile,
            Changes("api key: private", "api key: private"), "api key: private"));
        Assert.AreEqual(1, profile.Entries.Count);
        Assert.ThrowsException<JsonException>(() => AdminChatMemoryDocument.Merge(profile, "not json", "hello"));
        Assert.IsFalse(AdminChatMemoryDocument.Merge(profile, "[]", "hello"));
    }

    [TestMethod]
    public void Merge_UpdatesAndDeletesAutomaticEntries_NeverReplacesPinnedOnes()
    {
        var profile = new AdminChatMemoryProfile();
        AdminChatMemoryDocument.Merge(profile, Changes(), "Prefer concise replies");
        AdminChatMemoryDocument.Merge(profile, Changes("Prefer detailed replies", "Prefer detailed replies"), "Prefer detailed replies");
        Assert.AreEqual(1, profile.Entries.Count);
        Assert.AreEqual("Prefer detailed replies", profile.Entries[0].Text);
        profile.Entries[0] = profile.Entries[0] with { Pinned = true };
        Assert.IsFalse(AdminChatMemoryDocument.Merge(profile, Changes(), "Prefer concise replies"));
        profile.Entries[0] = profile.Entries[0] with { Pinned = false };
        Assert.IsTrue(AdminChatMemoryDocument.Merge(profile, Changes(evidence: "forget response style", remove: true), "forget response style"));
        Assert.AreEqual(0, profile.Entries.Count);
    }

    [TestMethod]
    public void PromptSelection_PublishedHighestAverageWins_DraftsDoNotOverride()
    {
        PromptItem Item(string text, decimal average, decimal maximum, bool draft) =>
            new PromptItem(1, "2026.11.02.1", "1", 1, "", new PromptItem_AddRequest
            {
                Content = text, ModelId = 0, IsDraft = draft
            }).UpdateEvalAvgScore(average).UpdateEvalMaxScore(maximum);
        var baseline = Item("default", 0, 0, false);
        var best = Item("improved", 90, 95, false);
        var draft = Item("draft", 100, 100, true);
        var peak = Item("lower-average", 80, 100, false);
        Assert.AreEqual("improved", MemoryPromptProvider.SelectPublishedPrompt(new[] { baseline, best, draft, peak }).Content);
        Assert.AreEqual("default", MemoryPromptProvider.SelectPublishedPrompt(new[] { baseline, draft }).Content);
        Assert.ThrowsException<NcfExceptionBase>(() => MemoryPromptProvider.SelectPublishedPrompt(new[] { draft }));
    }

    private AdminChatMemoryService Service()
    {
        var services = new ServiceCollection().AddSingleton<IMemoryPromptProvider>(new TestPromptProvider()).BuildServiceProvider();
        return new AdminChatMemoryService(_store, null!, services, NullLogger<AdminChatMemoryService>.Instance);
    }

    [TestMethod]
    public async Task AutomaticUpdate_OptOutNeverCallsModel()
    {
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, new(), false, "");
        await service.CompleteAsync(context, "Prefer concise replies", 0,
            (_, _, _) => throw new AssertFailedException("Must not call AI when Memory is off"), default);
        Assert.AreEqual("disabled", service.UpdateStatus);
    }

    [TestMethod]
    public async Task AutomaticUpdate_PersistsValidatedChange()
    {
        var profile = await _store.UpdateAsync(1, 12, "", value => value.Sessions[3] = true);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), "Prefer concise replies", 0,
            (_, input, _) =>
            {
                StringAssert.Contains(input, "currentUserMessage");
                Assert.IsFalse(input.Contains("assistant"));
                return Task.FromResult(Changes());
            }, default);
        Assert.AreEqual("updated", service.UpdateStatus);
        Assert.AreEqual(1, (await _store.ReadAsync(1, 12)).Entries.Count);
    }

    [TestMethod]
    public async Task AutomaticUpdate_ManualClearDuringGenerationWins()
    {
        var profile = await _store.UpdateAsync(1, 12, "", value => value.Sessions[3] = true);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), "Prefer concise replies", 0,
            async (_, _, _) =>
            {
                await _store.UpdateAsync(1, 12, profile.Revision, current => current.Entries.Clear());
                return Changes();
            }, default);
        Assert.AreEqual(0, (await _store.ReadAsync(1, 12)).Entries.Count);
        Assert.AreEqual("failed", service.UpdateStatus);
    }

    [TestMethod]
    public async Task AutomaticUpdate_ProviderFailureReportsFailureWithoutSaving()
    {
        var profile = await _store.UpdateAsync(1, 12, "", value => value.Sessions[3] = true);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), "Prefer concise replies", 0,
            (_, _, _) => throw new System.ClientModel.ClientResultException("Rate limited"), default);
        Assert.AreEqual("failed", service.UpdateStatus);
        Assert.AreEqual(0, (await _store.ReadAsync(1, 12)).Entries.Count);
    }

    [TestMethod]
    public async Task AutomaticUpdate_ConcurrentOptOutSkipsGeneration()
    {
        var profile = await _store.UpdateAsync(1, 12, "", value => value.Sessions[3] = true);
        await _store.UpdateAsync(1, 12, profile.Revision, value => value.Sessions.Remove(3));
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), "Prefer concise replies", 0,
            (_, _, _) => throw new AssertFailedException("Opt-out must suppress extraction"), default);
        Assert.AreEqual("conflict", service.UpdateStatus);
        Assert.AreEqual(0, (await _store.ReadAsync(1, 12)).Entries.Count);
    }

    private sealed class TestPromptProvider : IMemoryPromptProvider
    {
        public Task<MemoryUpdatePrompt> GetAsync(int modelId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MemoryUpdatePrompt("Test updater", "test-T1-A1"));
    }

    private sealed class MemoryDb(DbContextOptions<MemoryDb> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AdminChatMemoryState>();
            builder.Entity<PromptItem>().ToTable("MemoryTestPromptItem",
                table => table.HasCheckConstraint("NoteMaxLength", "length(Note) <= 20"));
        }
    }

    [TestMethod]
    public async Task Database_ImportsLegacyOnce_EncryptsAndRejectsStaleRevision()
    {
        var legacy = await _store.UpdateAsync(1, 12, "", profile =>
        {
            profile.Entries = AdminChatMemoryDocument.Parse("- Private preference");
            profile.Sessions[3] = true;
        });
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new MemoryDb(new DbContextOptionsBuilder<MemoryDb>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var store = new AdminChatMemoryDatabaseStore(db, _store, _protection);
        var migrated = await store.ReadAsync(1, 12);
        Assert.AreEqual(legacy.Revision, migrated.Revision);
        Assert.AreEqual(AdminChatMemoryMode.Learn, migrated.ModeFor(3));
        var row = await db.Set<AdminChatMemoryState>().AsNoTracking().SingleAsync();
        Assert.IsFalse(Encoding.UTF8.GetString(row.Payload).Contains("Private preference"));
        Assert.AreEqual(0, (await store.ReadAsync(1, 13)).Entries.Count);
        Assert.AreEqual(0, (await store.ReadAsync(2, 12)).Entries.Count);
        Assert.AreEqual(0, (await store.ReadAsync(1, 0)).Entries.Count);
        var id = migrated.Entries[0].Id;
        Assert.AreEqual(id, (await store.ReadAsync(1, 12)).Entries[0].Id);
        var saved = await store.UpdateAsync(1, 12, migrated.Revision, profile =>
        {
            profile.Entries.Clear();
            profile.SessionModes[3] = AdminChatMemoryMode.ReadOnly;
            profile.SystemMessage = new string('x', 40000);
        });
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() =>
            store.UpdateAsync(1, 12, migrated.Revision, profile => profile.SystemMessage = "stale"));
        var reopened = await new AdminChatMemoryDatabaseStore(db, _store, _protection).ReadAsync(1, 12);
        Assert.AreEqual(saved.Revision, reopened.Revision);
        Assert.AreEqual(0, reopened.Entries.Count, "A deleted profile must not resurrect its legacy file.");
        Assert.AreEqual(AdminChatMemoryMode.ReadOnly, reopened.ModeFor(3));
        Assert.AreEqual(40000, reopened.SystemMessage.Length, "Encrypted payload must not use a short binary column.");
    }

    [TestMethod]
    public async Task DefaultPrompt_SavesInRelationalDatabase_WithProductionLengthConstraints()
    {
        var item = MemoryPromptProvider.CreateDefaultPrompt(1, "2026.11.02.1", 0);
        foreach (var property in typeof(PromptItem).GetProperties())
        {
            var limit = property.GetCustomAttributes(typeof(MaxLengthAttribute), true).Cast<MaxLengthAttribute>().FirstOrDefault();
            if (limit != null && property.GetValue(item) is string text)
                Assert.IsTrue(text.Length <= limit.Length, property.Name + " exceeds the database length.");
        }

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new MemoryDb(new DbContextOptionsBuilder<MemoryDb>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Add(item);
        await db.SaveChangesAsync();
        Assert.AreEqual(MemoryPromptProvider.DefaultPrompt, (await db.Set<PromptItem>().SingleAsync()).Content);
        await Assert.ThrowsExceptionAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE MemoryTestPromptItem SET Note = 'System default: AdminChat memory updater. Publish and score a new version.'"));
    }

    [TestMethod]
    public async Task SqliteMigration_CreatesWorkingMemoryStoreAndUniqueOwnerIndex()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new MemoryDb(new DbContextOptionsBuilder<MemoryDb>().UseSqlite(connection).Options);
        var migration = new Senparc.Areas.Admin.Domain.Migrations.Sqlite.Add_AdminChat_Memory_State();
        var commands = db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.Model);
        foreach (var command in commands) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var store = new AdminChatMemoryDatabaseStore(db, _store, _protection);
        var first = await store.UpdateAsync(1, 12, "", profile => profile.SystemMessage = "new database");
        Assert.AreEqual(first.Revision, (await store.ReadAsync(1, 12)).Revision);
        db.Add(new AdminChatMemoryState { TenantId = 1, UserId = 12, Revision = "duplicate", Payload = [1] });
        await Assert.ThrowsExceptionAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [TestMethod]
    public void Retrieval_ScopeExpiryOverridesAndActualTokenizerBudget()
    {
        var profile = new AdminChatMemoryProfile();
        profile.Entries.Add(new("language", "Global language preference", "", "core", false, DateTimeOffset.UtcNow));
        profile.Entries.Add(new("language", "Workspace language preference", "", "core", false, DateTimeOffset.UtcNow) { WorkspaceId = "alpha" });
        profile.Entries.Add(new("other", "Other workspace private data", "", "core", false, DateTimeOffset.UtcNow) { WorkspaceId = "beta" });
        profile.Entries.Add(new("expired", "Expired preference", "", "core", false, DateTimeOffset.UtcNow) { ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) });
        profile.Entries.Add(new("archived", "Archived preference", "", "archive", false, DateTimeOffset.UtcNow));
        var result = AdminChatMemoryRetrieval.Build(profile, "alpha", "", "OpenAI:gpt-4o");
        Assert.AreEqual("o200k_base", result.Usage.Counter);
        StringAssert.Contains(result.Context, "Workspace language");
        Assert.IsFalse(result.Context.Contains("Global language") || result.Context.Contains("Other workspace")
            || result.Context.Contains("Expired") || result.Context.Contains("Archived"));
        Assert.AreEqual(1, result.Usage.Entries.Count);
        Assert.AreEqual(profile.Entries[1].Id, result.Usage.Entries[0].Id);
        for (var index = 0; index < 70; index++)
            profile.Entries.Add(new("topic-" + index, "数据库查询" + new string('字', 150), "数据库", "topic", false, DateTimeOffset.UtcNow));
        result = AdminChatMemoryRetrieval.Build(profile, "alpha", "数据库查询", "gpt-4o");
        Assert.IsTrue(result.Usage.Tokens <= 1000 && result.Usage.Tokens > 300);
        Assert.IsTrue(result.Usage.Entries.Any(entry => entry.Reason == "lexical-match"));
        Assert.AreEqual(Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForEncoding("o200k_base").CountTokens(result.Context),
            result.Usage.Tokens);
        var estimated = AdminChatMemoryRetrieval.Build(profile, "alpha", "数据库查询", "custom-model");
        Assert.AreEqual("utf8-conservative-estimate", estimated.Usage.Counter);
        Assert.AreEqual(Encoding.UTF8.GetByteCount(estimated.Context), estimated.Usage.Tokens);
        Assert.IsTrue(estimated.Usage.Tokens <= estimated.Usage.Budget);
    }

    [TestMethod]
    public async Task ReadOnly_AndOrdinaryTurns_NeverCallExtractorOrWrite()
    {
        var profile = await _store.UpdateAsync(1, 12, "", value => value.SessionModes[3] = AdminChatMemoryMode.ReadOnly);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, "", AdminChatMemoryMode.ReadOnly),
            "Remember: prefer concise replies", 0, (_, _, _) => throw new AssertFailedException("Read-only called AI."), default);
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), "What does this function do?", 0,
            (_, _, _) => throw new AssertFailedException("Ordinary turn called AI."), default);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
        Assert.IsFalse(AdminChatMemoryRetrieval.ShouldLearn("不要记住这条信息"));
        Assert.IsFalse(AdminChatMemoryRetrieval.ShouldLearn("Don't store this preference"));
        Assert.IsTrue(AdminChatMemoryRetrieval.ShouldLearn("以后请使用中文回复"));
    }

    [TestMethod]
    public void Merge_PersistsEvidenceAndStableIds_DeletionPurgesHistoryContent()
    {
        var profile = new AdminChatMemoryProfile();
        AdminChatMemoryDocument.Merge(profile, Changes(), "Prefer concise replies", "", 123, 42);
        var first = profile.Entries.Single();
        Assert.AreEqual(123, first.SourceMessageId);
        Assert.AreEqual(42, first.SourceSessionId);
        Assert.AreEqual("Prefer concise replies", first.Evidence);
        AdminChatMemoryDocument.Merge(profile, Changes("Prefer detailed replies", "Prefer detailed replies"), "Prefer detailed replies", "", 124, 42);
        Assert.AreEqual(first.Id, profile.Entries.Single().Id);
        Assert.AreEqual(2, profile.Entries.Single().Version);
        Assert.AreEqual(2, profile.History.Count);
        AdminChatMemoryDocument.Merge(profile, Changes(evidence: "forget response style", remove: true), "forget response style");
        Assert.AreEqual(0, profile.Entries.Count);
        Assert.AreEqual(1, profile.History.Count);
        Assert.AreEqual("", profile.History.Single().Before.Text);
        Assert.AreEqual("", profile.History.Single().Before.Evidence);
    }

    [TestMethod]
    public void Merge_RejectsWorkspaceDataWithoutWorkspace_AndImplicitDeletion()
    {
        var profile = new AdminChatMemoryProfile();
        var json = JsonSerializer.Serialize(new[] { new {
            key = "test-command", text = "Use dotnet test", topic = "tests", tier = "topic",
            evidence = "Remember: use dotnet test", remove = false, scope = "workspace", kind = "project"
        }});
        Assert.ThrowsException<AdminChatMemoryValidationException>(() => AdminChatMemoryDocument.Merge(profile, json, "Remember: use dotnet test"));
        Assert.IsTrue(AdminChatMemoryDocument.Merge(profile, json, "Remember: use dotnet test", "alpha", 1, 2));
        Assert.AreEqual("alpha", profile.Entries.Single().WorkspaceId);
        Assert.ThrowsException<AdminChatMemoryValidationException>(() => AdminChatMemoryDocument.Merge(profile,
            Changes(remove: true), "Prefer concise replies"));
    }

    [TestMethod]
    public async Task AutomaticUpdate_ReportedTimePreference_PersistsInDatabaseAndLoadsNextTurn()
    {
        const string message = "以后都要记住这个规则（对话结束后返回当前时间）";
        const string preference = "每次回复结束时返回当前时间；无实时工具时说明无法获取，不虚构时间。";
        Assert.IsTrue(AdminChatMemoryRetrieval.ShouldLearn(message));
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new MemoryDb(new DbContextOptionsBuilder<MemoryDb>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var store = new AdminChatMemoryDatabaseStore(db, _store, _protection);
        var profile = await store.UpdateAsync(1, 12, "", value => value.SessionModes[3] = AdminChatMemoryMode.Learn);
        using var services = new ServiceCollection().AddSingleton<IMemoryPromptProvider>(new TestPromptProvider()).BuildServiceProvider();
        var service = new AdminChatMemoryService(store, null!, services, NullLogger<AdminChatMemoryService>.Instance);
        await service.CompleteAsync(new(1, 12, 3, profile, true, "", AdminChatMemoryMode.Learn, "alpha", 123), message, 0,
            (_, input, _) =>
            {
                StringAssert.Contains(input, message);
                return Task.FromResult(JsonSerializer.Serialize(new[] {
                    new { key = "reply-current-time", text = preference, topic = "", tier = "core",
                        evidence = message, remove = false, scope = "global", kind = "preference" }
                }));
            }, default);
        Assert.AreEqual("updated", service.UpdateResult.Status);
        Assert.AreEqual("saved", service.UpdateResult.Reason);
        Assert.AreEqual(1, service.UpdateResult.GlobalChanges);
        Assert.AreEqual(0, service.UpdateResult.WorkspaceChanges);
        Assert.IsTrue(service.UpdateResult.Requested);
        var saved = await store.ReadAsync(1, 12);
        StringAssert.Contains(AdminChatMemoryDocument.Export(saved.Entries), preference);
        Assert.AreEqual(123, saved.Entries.Single().SourceMessageId);
        StringAssert.Contains(AdminChatMemoryRetrieval.Build(saved, "other-workspace", "你好", "gpt-4o").Context, preference);
        Assert.IsFalse(Directory.Exists(Path.Combine(_root, "alpha")), "Private Memory does not create shared Workspace files.");
    }

    [TestMethod]
    public async Task UpdateResult_DistinguishesEmptyPinnedInvalidAndSavedWorkspace()
    {
        const string message = "Remember: Prefer concise replies";
        var profile = await _store.UpdateAsync(1, 12, "", value => value.SessionModes[3] = AdminChatMemoryMode.Learn);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), message, 0, (_, _, _) => Task.FromResult("[]"), default);
        Assert.AreEqual("no-candidates", service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.GlobalChanges);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
        Assert.AreEqual("test-T1-A1", service.UpdateResult.PromptCode);
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), message, 0,
            (_, _, _) => Task.FromResult("not json"), default);
        Assert.AreEqual("invalid-json", service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.GlobalChanges);
        profile = await _store.UpdateAsync(1, 12, profile.Revision, value => value.Entries.Add(
            new("response-style", "Prefer concise replies", "", "core", true, DateTimeOffset.UtcNow)));
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), message, 0,
            (_, _, _) => Task.FromResult(Changes()), default);
        Assert.AreEqual("pinned", service.UpdateResult.Reason);
        Assert.AreEqual("unchanged", service.UpdateResult.Status);
        var workspaceOutput = JsonSerializer.Serialize(new[] {
            new { key = "response-style", text = "Prefer concise replies", topic = "", tier = "core",
                evidence = "Prefer concise replies", remove = false, scope = "workspace", kind = "preference" }
        });
        await service.CompleteAsync(new(1, 12, 3, profile, true, "", AdminChatMemoryMode.Learn, "alpha"), message, 0,
            (_, _, _) => Task.FromResult(workspaceOutput), default);
        Assert.AreEqual("updated", service.UpdateResult.Status);
        Assert.AreEqual(0, service.UpdateResult.GlobalChanges);
        Assert.AreEqual(1, service.UpdateResult.WorkspaceChanges);
        var saved = await _store.ReadAsync(1, 12);
        Assert.AreEqual(1, saved.Entries.Count(entry => entry.WorkspaceId == "alpha"));
        Assert.AreEqual(0, AdminChatMemoryRetrieval.Eligible(saved, "beta").Count(entry => entry.WorkspaceId == "alpha"));
    }

    [TestMethod]
    public void PromptUpgrade_ChangesOnlyUntouchedUnscoredDefault_AndKeepsCustomScores()
    {
        var item = MemoryPromptProvider.CreateDefaultPrompt(1, "2026.11.03.1", 0);
        item.UpdateContent(MemoryPromptProvider.PreviousDefaultPrompt);
        Assert.IsTrue(MemoryPromptProvider.UpgradeUnmodifiedDefault(item));
        Assert.AreEqual(MemoryPromptProvider.DefaultPrompt, item.Content);
        Assert.IsFalse(MemoryPromptProvider.UpgradeUnmodifiedDefault(item), "Upgrade must be idempotent.");
        item.UpdateContent("Custom prompt");
        Assert.IsFalse(MemoryPromptProvider.UpgradeUnmodifiedDefault(item));
        item.UpdateContent(MemoryPromptProvider.PreviousDefaultPrompt).UpdateEvalAvgScore(90);
        Assert.IsFalse(MemoryPromptProvider.UpgradeUnmodifiedDefault(item), "Never overwrite a user-scored prompt.");
        StringAssert.Contains(MemoryPromptProvider.DefaultPrompt, "以后都要记住这个规则");
        StringAssert.Contains(AdminChatMemoryService.StorageInstructions(AdminChatMemoryMode.Learn), "AFTER your reply");
        StringAssert.Contains(AdminChatMemoryService.StorageInstructions(AdminChatMemoryMode.Off), "Never claim");
    }
}
