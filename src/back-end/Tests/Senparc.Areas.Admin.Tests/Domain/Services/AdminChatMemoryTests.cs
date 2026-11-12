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

    private static string Extraction(string changes, string intent = "update")
    {
        using var document = JsonDocument.Parse(changes);
        return JsonSerializer.Serialize(new { intent, changes = document.RootElement });
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
                return Task.FromResult(Extraction(Changes()));
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
                return Extraction(Changes());
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
        Assert.IsFalse(AdminChatMemoryRetrieval.ShouldLearn("不要记下这条信息"));
        Assert.IsFalse(AdminChatMemoryRetrieval.ShouldLearn("Don't record that"));
        Assert.IsFalse(AdminChatMemoryRetrieval.ShouldLearn("Do not note it down"));
        Assert.IsTrue(AdminChatMemoryRetrieval.ShouldLearn("以后请使用中文回复"));
    }

    [DataTestMethod]
    [DataRow("我会给你定义一些规则，到时候请你记住，让我们开始", "defer", "intent-deferred")]
    [DataRow("I will give you some rules; remember them when I send them. Let's begin.", "defer", "intent-deferred")]
    [DataRow("记住这条规则：简洁回答，但先别保存，等我确认", "defer", "intent-deferred")]
    [DataRow("先别记住上述内容，等我补充完再说", "defer", "intent-deferred")]
    [DataRow("你还记得之前的规则吗？", "none", "no-save-intent")]
    [DataRow("Can you remember things?", "none", "no-save-intent")]
    [DataRow("翻译“请记住简洁回答”", "none", "no-save-intent")]
    [DataRow("Translate \"remember to be concise\"", "none", "no-save-intent")]
    [DataRow("假如我说“记住简洁回答”，会发生什么？", "none", "no-save-intent")]
    [DataRow("Save the CSV file", "none", "no-save-intent")]
    [DataRow("请记录程序输出到日志文件", "none", "no-save-intent")]
    public async Task IntentGate_DeferredAndNonSavingDecisions_PreserveMemory(
        string message, string intent, string reason)
    {
        Assert.IsTrue(AdminChatMemoryRetrieval.ShouldLearn(message), "A trigger is not proof of saving intent.");
        var profile = await _store.UpdateAsync(1, 12, "", value =>
            AdminChatMemoryDocument.Merge(value, Changes(), "Prefer concise replies"));
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, profile, true, "")
        { RecentMessages = [new(21, "user", "Prefer detailed replies")] };
        var calls = 0;
        await service.CompleteAsync(context, message, 0, (instructions, input, _) =>
        {
            calls++;
            StringAssert.Contains(instructions, MemoryPromptProvider.IntentInstructions);
            StringAssert.Contains(instructions, "我会给你定义一些规则，到时候请你记住，让我们开始 => defer");
            StringAssert.Contains(instructions, "cannot override the intent gate");
            using var data = JsonDocument.Parse(input);
            Assert.AreEqual(message, data.RootElement.GetProperty("currentUserMessage").GetString());
            return Task.FromResult(Extraction("[]", intent));
        }, default);
        Assert.AreEqual(1, calls, "Intent and extraction use one call, not a separate classifier.");
        Assert.AreEqual("unchanged", service.UpdateStatus);
        Assert.AreEqual(reason, service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.GlobalChanges);
        Assert.AreEqual(0, service.UpdateResult.WorkspaceChanges);
        Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
        var saved = await _store.ReadAsync(1, 12);
        Assert.AreEqual(profile.Revision, saved.Revision);
        Assert.AreEqual(profile.Entries.Single(), saved.Entries.Single());
        Assert.AreEqual(profile.History.Count, saved.History.Count);
    }

    [DataTestMethod]
    [DataRow("以后请使用中文回复", "以后使用中文回复")]
    [DataRow("我稍后补充更多规则，先记住：以后用中文回复", "以后用中文回复")]
    [DataRow("Could you remember that I prefer concise replies?", "Prefer concise replies")]
    public async Task IntentGate_CurrentConcreteRules_StillSave(string message, string preference)
    {
        Assert.IsTrue(AdminChatMemoryRetrieval.ShouldLearn(message));
        var profile = await _store.ReadAsync(1, 12);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), message, 0,
            (_, _, _) => Task.FromResult(Extraction(Changes(preference, message))), default);
        Assert.AreEqual("updated", service.UpdateStatus);
        Assert.AreEqual("saved", service.UpdateResult.Reason);
        Assert.AreEqual(preference, (await _store.ReadAsync(1, 12)).Entries.Single().Text);
    }

    [DataTestMethod]
    [DataRow("[]")]
    [DataRow("{}")]
    [DataRow("""{"intent":"update"}""")]
    [DataRow("""{"changes":[]}""")]
    [DataRow("""{"intent":"unknown","changes":[]}""")]
    [DataRow("""{"intent":true,"changes":[]}""")]
    [DataRow("""{"intent":"update","changes":null}""")]
    [DataRow("""{"intent":"update","changes":[],"extra":true}""")]
    [DataRow("""{"intent":"defer","intent":"update","changes":[]}""")]
    [DataRow("""{"intent":"defer","changes":[{}]}""")]
    [DataRow("""{"intent":"none","changes":[{}]}""")]
    public async Task IntentGate_MissingInvalidOrContradictoryDecision_FailsWithoutWriting(string output)
    {
        var profile = await _store.ReadAsync(1, 12);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), "Remember: prefer concise replies", 0,
            (_, _, _) => Task.FromResult(output), default);
        Assert.AreEqual("failed", service.UpdateStatus);
        Assert.AreEqual("invalid-change", service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.GlobalChanges);
        Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
    }

    [TestMethod]
    public async Task IntentGate_DeferredDecisionCannotSmuggleOtherwiseValidChanges()
    {
        var profile = await _store.ReadAsync(1, 12);
        var service = Service();
        foreach (var intent in new[] { "defer", "none" })
        {
            await service.CompleteAsync(new(1, 12, 3, profile, true, ""), "Remember: Prefer concise replies", 0,
                (_, _, _) => Task.FromResult(Extraction(Changes(), intent)), default);
            Assert.AreEqual("invalid-change", service.UpdateResult.Reason);
            Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
            Assert.AreEqual(0, (await _store.ReadAsync(1, 12)).Entries.Count);
        }
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
                return Task.FromResult(Extraction(JsonSerializer.Serialize(new[] {
                    new { key = "reply-current-time", text = preference, topic = "", tier = "core",
                        evidence = message, remove = false, scope = "global", kind = "preference" }
                })));
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
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), message, 0, (_, _, _) => Task.FromResult(Extraction("[]")), default);
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
            (_, _, _) => Task.FromResult(Extraction(Changes())), default);
        Assert.AreEqual("pinned", service.UpdateResult.Reason);
        Assert.AreEqual("unchanged", service.UpdateResult.Status);
        var workspaceOutput = JsonSerializer.Serialize(new[] {
            new { key = "response-style", text = "Prefer concise replies", topic = "", tier = "core",
                evidence = "Prefer concise replies", remove = false, scope = "workspace", kind = "preference" }
        });
        await service.CompleteAsync(new(1, 12, 3, profile, true, "", AdminChatMemoryMode.Learn, "alpha"), message, 0,
            (_, _, _) => Task.FromResult(Extraction(workspaceOutput)), default);
        Assert.AreEqual("updated", service.UpdateResult.Status);
        Assert.AreEqual(0, service.UpdateResult.GlobalChanges);
        Assert.AreEqual(1, service.UpdateResult.WorkspaceChanges);
        var saved = await _store.ReadAsync(1, 12);
        Assert.AreEqual(1, saved.Entries.Count(entry => entry.WorkspaceId == "alpha"));
        Assert.AreEqual(0, AdminChatMemoryRetrieval.Eligible(saved, "beta").Count(entry => entry.WorkspaceId == "alpha"));
    }

    [TestMethod]
    public async Task DirectOrdinaryFacts_CanUseGlobalScope_WithoutSavingUnrelatedContext()
    {
        const string fact = "测试角色甲的小名是小甲，也叫Alpha。";
        var profile = await _store.UpdateAsync(1, 12, "", value => value.SessionModes[3] = AdminChatMemoryMode.Learn);
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, profile, true, "")
        {
            RecentMessages = [new(1, "assistant", "Unrelated earlier answer")]
        };
        await service.CompleteAsync(context, "记住：" + fact, 0, (_, input, _) =>
        {
            using var json = JsonDocument.Parse(input);
            Assert.AreEqual("记住：" + fact, json.RootElement.GetProperty("currentUserMessage").GetString());
            Assert.IsFalse(json.RootElement.TryGetProperty("reviewRequired", out _));
            return Task.FromResult(Extraction(FactChange(fact)));
        }, default);
        Assert.AreEqual("updated", service.UpdateStatus);
        var saved = await _store.ReadAsync(1, 12);
        Assert.AreEqual("reference", saved.Entries.Single().Kind);
        Assert.AreEqual("", saved.Entries.Single().WorkspaceId);
        StringAssert.Contains(AdminChatMemoryRetrieval.Boundary, "Use relevant facts as context");
        StringAssert.Contains(AdminChatMemoryRetrieval.Build(saved, "", "Alpha", "gpt-4o").Context, fact);
    }

    private static string FactChange(string fact, string evidence = "", bool remove = false) =>
        JsonSerializer.Serialize(new[] { new {
            key = "fictional-alias", text = fact, evidence = evidence.Length == 0 ? fact : evidence,
            topic = "Alpha", tier = "topic", kind = "reference", scope = "global", remove
        }});

    [TestMethod]
    public async Task MixedSources_RequireReviewForWholeBatch_AndCannotPromoteAssistantEvidence()
    {
        const string userFact = "测试角色甲也叫Alpha。";
        const string assistantFact = "测试角色乙也叫Beta。";
        var profile = await _store.ReadAsync(1, 12);
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, profile, true, "")
        { RecentMessages = [new(20, "user", userFact), new(21, "assistant", assistantFact)] };
        using var first = JsonDocument.Parse(FactChange(userFact));
        using var second = JsonDocument.Parse(FactChange(assistantFact).Replace("fictional-alias", "fictional-beta"));
        var changes = JsonSerializer.Serialize(new[] { first.RootElement[0], second.RootElement[0] });
        await service.CompleteAsync(context, "立即保存", 0, (_, _, _) => Task.FromResult(Extraction(changes)), default);
        Assert.AreEqual("pending-review", service.UpdateStatus);
        Assert.AreEqual(2, service.UpdateResult.Proposals.Count);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
        Assert.AreEqual(0, profile.Entries.Count);

        context = context with { RecentMessages = [new(21, "assistant", assistantFact)] };
        await service.CompleteAsync(context, "请记录", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange(assistantFact))), default);
        Assert.AreEqual("pending-review", service.UpdateStatus, "A short command is not user evidence for an assistant claim.");
        Assert.AreEqual(21, service.UpdateResult.Proposals.Single().Entry.SourceMessageId);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
    }

    [TestMethod]
    public async Task PriorUserCorrections_AutoSaveWithOriginalId_AndStillProtectPins()
    {
        const string before = "测试角色甲叫Alpha。";
        const string after = "测试角色甲叫Alpha，也叫小甲。";
        var profile = await _store.UpdateAsync(1, 12, "", value =>
            AdminChatMemoryDocument.Merge(value, FactChange(before), before));
        var original = profile.Entries.Single();
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, profile, true, "")
        { RecentMessages = [new(20, "user", after), new(21, "assistant", after)] };
        await service.CompleteAsync(context, "请记录", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange(after))), default);
        var saved = await _store.ReadAsync(1, 12);
        Assert.AreEqual("updated", service.UpdateStatus);
        Assert.AreEqual(original.Id, saved.Entries.Single().Id);
        Assert.AreEqual(original.Version + 1, saved.Entries.Single().Version);
        Assert.AreEqual(20, saved.Entries.Single().SourceMessageId, "Prefer the actual user assertion over its assistant echo.");
        profile = await _store.UpdateAsync(1, 12, saved.Revision, value =>
            value.Entries[0] = value.Entries[0] with { Pinned = true });
        context = context with { Profile = profile, RecentMessages = [new(22, "user", before)] };
        await service.CompleteAsync(context, "请记录", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange(before))), default);
        Assert.AreEqual("pinned", service.UpdateResult.Reason);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
    }

    [DataTestMethod]
    [DataRow("user", "updated")]
    [DataRow("assistant", "pending-review")]
    public async Task ReferencedFacts_UseActualEvidenceRoleToDecideReview(string role, string expectedStatus)
    {
        const string fact = "测试角色甲的小名是小甲，也叫Alpha。";
        var profile = await _store.ReadAsync(1, 12);
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, profile, true, "")
        { RecentMessages = [new(21, role, fact)] };
        await service.CompleteAsync(context, "记住上述内容", 0, (_, input, _) =>
        {
            using var json = JsonDocument.Parse(input);
            Assert.IsFalse(json.RootElement.TryGetProperty("reviewRequired", out _));
            Assert.AreEqual(role, json.RootElement.GetProperty("recentMessages")[0].GetProperty("Role").GetString());
            return Task.FromResult(Extraction(FactChange(fact)));
        }, default);
        Assert.AreEqual(expectedStatus, service.UpdateStatus);
        if (role == "user")
        {
            var saved = (await _store.ReadAsync(1, 12)).Entries.Single();
            Assert.AreEqual(fact, saved.Text);
            Assert.AreEqual(21, saved.SourceMessageId);
            Assert.AreEqual("user-message", saved.Source);
            Assert.AreEqual(1, service.UpdateResult.GlobalChanges);
            Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
            return;
        }
        Assert.AreEqual(profile.Revision, service.UpdateResult.Revision);
        Assert.AreEqual(0, service.UpdateResult.GlobalChanges);
        var proposal = service.UpdateResult.Proposals.Single();
        Assert.AreEqual(fact, proposal.Entry.Text);
        Assert.AreEqual("", proposal.Entry.Id);
        Assert.AreEqual(0, proposal.Entry.Version);
        Assert.IsTrue(proposal.Entry.Pinned);
        Assert.AreEqual(0, profile.Entries.Count, "Review must not mutate the turn snapshot.");
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
        Assert.AreEqual(0, (await _store.ReadAsync(1, 12)).Entries.Count, "No approval means no persistence.");
    }

    [DataTestMethod]
    [DataRow("请记录")]
    [DataRow("立即保存")]
    [DataRow("请你使用 “审查候选记忆”功能 进行保存")]
    [DataRow("Please save it now.")]
    [DataRow("Record that.")]
    [DataRow("Note it down.")]
    public async Task ShortSaveCommands_WithoutResolvableContext_DoNotInventFacts(string message)
    {
        Assert.IsTrue(AdminChatMemoryRetrieval.ShouldLearn(message));
        Assert.IsTrue(AdminChatMemoryRetrieval.NeedsConversationContext(message));
        var profile = await _store.ReadAsync(1, 12);
        var service = Service();
        await service.CompleteAsync(new(1, 12, 3, profile, true, ""), message, 0,
            (_, _, _) => Task.FromResult(Extraction("[]")), default);
        Assert.AreEqual("unresolved-reference", service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
    }

    [TestMethod]
    public async Task ContextCandidates_RejectUnsupportedEvidenceAndSecrets_AndExplainUnresolvedReferences()
    {
        var profile = await _store.ReadAsync(1, 12);
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, profile, true, "")
        { RecentMessages = [new(21, "assistant", "Unrelated text")] };
        await service.CompleteAsync(context, "记住上述内容", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange("Unsupported fact"))), default);
        Assert.AreEqual("invalid-change", service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
        context = context with { RecentMessages = [new(22, "user", "password=not-a-real-credential")] };
        await service.CompleteAsync(context, "记住上述内容", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange("password=not-a-real-credential"))), default);
        Assert.AreEqual("invalid-change", service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
        await service.CompleteAsync(context, "记住上述内容", 0, (_, _, _) => Task.FromResult(Extraction("[]")), default);
        Assert.AreEqual("unresolved-reference", service.UpdateResult.Reason);
        Assert.AreEqual(profile.Revision, (await _store.ReadAsync(1, 12)).Revision);
        Assert.IsFalse(AdminChatMemoryRetrieval.NeedsConversationContext("不要记住上述内容"));
        Assert.IsTrue(AdminChatMemoryRetrieval.NeedsConversationContext("Remember that."));
        Assert.IsTrue(AdminChatMemoryRetrieval.ShouldLearn("更新记忆：测试规则"));
        Assert.IsFalse(AdminChatMemoryRetrieval.NeedsConversationContext("以后都要记住这个规则（对话结束后返回当前时间）"));
    }

    [TestMethod]
    public void RecentMessageSelection_IsBoundedAndExcludesSystemAndOversizedContent()
    {
        var messages = Enumerable.Range(1, 8).Select(id =>
            new AdminChatMessage(3, ChatMessageRoleType.User, new string('a', 1900), id) { Id = id }).ToList();
        var selected = AdminChatMemoryService.SelectRecentMessages(messages);
        Assert.AreEqual(3, selected.Count);
        Assert.IsTrue(selected.Sum(source => source.Text.Length) <= 6000);
        CollectionAssert.AreEqual(new[] { 6, 7, 8 }, selected.Select(source => source.MessageId).ToArray());
        messages.Add(new(3, ChatMessageRoleType.System, "Not context", 9) { Id = 9 });
        messages.Add(new(3, ChatMessageRoleType.Assistant, new string('b', 2001), 10) { Id = 10 });
        selected = AdminChatMemoryService.SelectRecentMessages(messages);
        Assert.IsFalse(selected.Any(source => source.MessageId is 9 or 10));
        Assert.IsTrue(selected.All(source => source.MessageId >= 5), "Do not search beyond the six-message window.");
    }

    [TestMethod]
    public async Task ReviewCorrections_KeepOriginalVersion_AndNeverSilentlyDeleteOrChangePins()
    {
        const string before = "测试角色甲叫Alpha。";
        const string after = "测试角色甲叫Alpha，也叫小甲。";
        var profile = await _store.UpdateAsync(1, 12, "", value =>
            AdminChatMemoryDocument.Merge(value, FactChange(before), before));
        var existing = profile.Entries.Single();
        var service = Service();
        var context = new AdminChatMemoryContext(1, 12, 3, profile, true, "")
        { RecentMessages = [new(21, "assistant", after)] };
        await service.CompleteAsync(context, "更正上述记忆", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange(after))), default);
        var proposal = service.UpdateResult.Proposals.Single();
        Assert.AreEqual(existing.Id, proposal.Entry.Id);
        Assert.AreEqual(existing.Version, proposal.Entry.Version, "Manual confirmation must use the current persisted version.");
        Assert.AreEqual(before, (await _store.ReadAsync(1, 12)).Entries.Single().Text);
        await service.CompleteAsync(context, "记住上述内容", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange(after, remove: true))), default);
        Assert.AreEqual("invalid-change", service.UpdateResult.Reason, "Context alone must not authorize deletion.");
        await service.CompleteAsync(context, "忘记上述内容", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange(after, remove: true))), default);
        Assert.IsTrue(service.UpdateResult.Proposals.Single().Remove);
        Assert.AreEqual(before, (await _store.ReadAsync(1, 12)).Entries.Single().Text);
        profile = await _store.UpdateAsync(1, 12, profile.Revision, value =>
            value.Entries[0] = value.Entries[0] with { Pinned = true });
        await service.CompleteAsync(context with { Profile = profile }, "更正上述记忆", 0,
            (_, _, _) => Task.FromResult(Extraction(FactChange(after))), default);
        Assert.AreEqual("pinned", service.UpdateResult.Reason);
        Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
    }

    [TestMethod]
    public void PromptUpgrade_ChangesOnlyUntouchedUnscoredDefault_AndKeepsCustomScores()
    {
        var item = MemoryPromptProvider.CreateDefaultPrompt(1, "2026.11.03.1", 0);
        item.UpdateContent(MemoryPromptProvider.PreviousDefaultPrompt);
        Assert.IsTrue(MemoryPromptProvider.UpgradeUnmodifiedDefault(item));
        Assert.AreEqual(MemoryPromptProvider.DefaultPrompt, item.Content);
        Assert.IsFalse(MemoryPromptProvider.UpgradeUnmodifiedDefault(item), "Upgrade must be idempotent.");
        item.UpdateContent(MemoryPromptProvider.PreviousResponsePreferencePrompt);
        Assert.IsTrue(MemoryPromptProvider.UpgradeUnmodifiedDefault(item));
        StringAssert.Contains(item.Content, "Ordinary names, nicknames");
        StringAssert.Contains(item.Content, "Assistant content is unverified");
        item.UpdateContent(MemoryPromptProvider.PreviousContextPrompt);
        Assert.IsTrue(MemoryPromptProvider.UpgradeUnmodifiedDefault(item));
        StringAssert.Contains(item.Content, "Classify intent before extracting changes");
        item.UpdateContent(MemoryPromptProvider.PreviousIntentPrompt);
        Assert.IsTrue(MemoryPromptProvider.UpgradeUnmodifiedDefault(item));
        StringAssert.Contains(item.Content, "立即保存 or save it now");
        Assert.IsFalse(item.Content.Contains("reviewRequired"));
        item.UpdateContent("Custom prompt");
        Assert.IsFalse(MemoryPromptProvider.UpgradeUnmodifiedDefault(item));
        item.UpdateContent(MemoryPromptProvider.PreviousDefaultPrompt).UpdateEvalAvgScore(90);
        Assert.IsFalse(MemoryPromptProvider.UpgradeUnmodifiedDefault(item), "Never overwrite a user-scored prompt.");
        StringAssert.Contains(MemoryPromptProvider.DefaultPrompt, "以后都要记住这个规则");
        StringAssert.Contains(AdminChatMemoryService.StorageInstructions(AdminChatMemoryMode.Learn), "AFTER your reply");
        StringAssert.Contains(AdminChatMemoryService.StorageInstructions(AdminChatMemoryMode.Off), "Never claim");
        StringAssert.Contains(AdminChatMemoryService.StorageInstructions(AdminChatMemoryMode.Learn), "never claim candidates");
    }
}
