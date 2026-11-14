using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatHistorySearchTests
{
    [TestMethod]
    public void Search_ReturnsNewestActiveFragments_AndSkipsArchivedDeletedAndOtherUsers()
    {
        var owner = Session(1, 12, "预算讨论");
        var archived = Session(2, 12, "预算归档");
        archived.Archive();
        var deleted = Session(3, 12, "预算已删除");
        deleted.Delete();
        var otherUser = Session(4, 13, "预算他人");
        var flagged = Session(5, 12, "预算隐藏");
        flagged.Flag = true;
        var titleOnly = Session(6, 12, "年度预算标题");
        var sessions = new[] { owner, archived, deleted, otherUser, flagged, titleOnly };
        var messages = new List<AdminChatMessage>();
        for (var index = 0; index < 12; index++)
        {
            var content = index == 11
                ? new string('前', 80) + "Budget" + new string('后', 80)
                : $"较早内容 {index} 处提到 Budget 计划";
            var message = Message(100 + index, owner.Id, content);
            message.AddTime = new DateTime(2026, 1, 1).AddMinutes(index);
            messages.Add(message);
        }
        var hidden = Message(7, owner.Id, "这条 Budget 已软删除");
        hidden.Flag = true;
        messages.Add(hidden);
        messages.Add(Message(8, archived.Id, "归档里的 Budget"));
        messages.Add(Message(9, deleted.Id, "删除里的 Budget"));
        messages.Add(Message(10, otherUser.Id, "别人的 Budget"));
        messages.Add(Message(11, flagged.Id, "隐藏会话的 Budget"));
        messages.Add(Message(12, titleOnly.Id, "这条正文没有关键字"));

        var hits = AdminChatHistorySearch.Search(sessions, messages, 12, " budget ", 20);

        Assert.AreEqual(10, hits.Count);
        Assert.IsTrue(hits.All(hit => hit.SessionId == owner.Id && hit.MatchKind == AdminChatHistorySearchMatchKind.Content));
        CollectionAssert.AreEqual(
            Enumerable.Range(0, 10).Select(index => 111 - index).ToArray(),
            hits.Select(hit => hit.MessageId).ToArray());
        Assert.AreEqual("Budget", hits[0].SnippetMatch);
        Assert.IsTrue(hits[0].SnippetBefore.StartsWith("…", StringComparison.Ordinal));
        Assert.IsTrue(hits[0].SnippetAfter.EndsWith("…", StringComparison.Ordinal));

        var titleHits = AdminChatHistorySearch.Search(sessions, messages, 12, "预算标题");
        Assert.AreEqual(1, titleHits.Count);
        Assert.AreEqual(AdminChatHistorySearchMatchKind.Title, titleHits[0].MatchKind);
        Assert.AreEqual(0, titleHits[0].MessageId);
        Assert.AreEqual("预算标题", titleHits[0].SnippetMatch);
        Assert.AreEqual(0, AdminChatHistorySearch.Search(sessions, messages, 12, "   ").Count);
    }

    [TestMethod]
    public void Search_PrefersMessageFragmentWhenTitleAlsoMatches()
    {
        var session = Session(3, 12, "预算讨论");
        var message = Message(9, session.Id, "请看这段预算说明");
        message.AddTime = new DateTime(2026, 3, 1);

        var hits = AdminChatHistorySearch.Search(new[] { session }, new[] { message }, 12, "预算");

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual(9, hits[0].MessageId);
        Assert.AreEqual(AdminChatHistorySearchMatchKind.Content, hits[0].MatchKind);
        Assert.IsTrue(hits[0].TitleMatched);
        Assert.AreEqual("预算", hits[0].SnippetMatch);
    }

    [TestMethod]
    public async Task SearchActiveHistory_ReadsOnlyCurrentUsersActiveSessions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection, 11);
        await db.Database.EnsureCreatedAsync();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var sessions = CreateSessionService(db, provider);
        var messages = CreateMessageService(db, provider);
        var visible = await sessions.CreateSessionAsync("可见会话", 12);
        var archived = await sessions.CreateSessionAsync("归档会话", 12);
        var otherUser = await sessions.CreateSessionAsync("其他用户", 13);
        var visibleMessage = await messages.AddMessageAsync(visible.Id, ChatMessageRoleType.User, "这里有可见关键字");
        await messages.AddMessageAsync(archived.Id, ChatMessageRoleType.Assistant, "归档关键字不应出现");
        await messages.AddMessageAsync(otherUser.Id, ChatMessageRoleType.User, "其他用户关键字");
        archived.Archive();
        await db.SaveChangesAsync();

        await using var otherTenantDb = CreateContext(connection, 22);
        var otherTenantSessions = CreateSessionService(otherTenantDb, provider);
        var otherTenantMessages = CreateMessageService(otherTenantDb, provider);
        var otherTenant = await otherTenantSessions.CreateSessionAsync("其他租户", 12);
        await otherTenantMessages.AddMessageAsync(otherTenant.Id, ChatMessageRoleType.User, "其他租户关键字");

        var hits = await messages.SearchActiveHistoryAsync(12, "关键字");

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual(visible.Id, hits[0].SessionId);
        Assert.AreEqual(visibleMessage.Id, hits[0].MessageId);
        Assert.AreEqual("关键字", hits[0].SnippetMatch);
        Assert.AreEqual("可见会话", hits[0].Title);
    }

    [TestMethod]
    public void SearchToolCalls_UsesOnlyHarnessCallNamesInsideActiveSessions()
    {
        var active = Session(1, 12, "进行中");
        var archived = Session(2, 12, "已归档");
        archived.Archive();
        var other = Session(3, 13, "别人");
        var activeTrajectory = Trajectory(10, active.Id, 12);
        var archivedTrajectory = Trajectory(11, archived.Id, 12);
        var otherTrajectory = Trajectory(12, other.Id, 13);
        var early = Event(activeTrajectory.Id, 2, "tool.call", "workspace_list");
        var later = Event(activeTrajectory.Id, 4, "tool.call", "workspace_search");
        var result = Event(activeTrajectory.Id, 5, "tool.result", "workspace_list", "workspace_list 的结果");
        var otherName = Event(activeTrajectory.Id, 6, "tool.call", "other_tool", "参数里提到 workspace_list");
        var earlierReply = Message(20, active.Id, "较早回复");
        earlierReply.SetTrajectory(activeTrajectory.Id, 3);
        var laterReply = Message(21, active.Id, "较晚回复");
        laterReply.SetTrajectory(activeTrajectory.Id, 8);

        var hits = AdminChatHistorySearch.SearchToolCalls(
            new[] { active, archived, other },
            new[] { activeTrajectory, archivedTrajectory, otherTrajectory },
            new[]
            {
                early, later, result, otherName,
                Event(archivedTrajectory.Id, 1, "tool.call", "workspace_list"),
                Event(otherTrajectory.Id, 1, "tool.call", "workspace_list")
            },
            new[] { earlierReply, laterReply },
            12,
            "WORKSPACE");

        Assert.AreEqual(2, hits.Count);
        Assert.AreEqual(4, hits[0].EventSequence);
        Assert.AreEqual(21, hits[0].MessageId);
        Assert.AreEqual("workspace", hits[0].SnippetMatch);
        Assert.AreEqual("_search", hits[0].SnippetAfter);
        Assert.AreEqual(2, hits[1].EventSequence);
        Assert.AreEqual(20, hits[1].MessageId);
        Assert.AreEqual(AdminChatHistorySearchMatchKind.ToolCall, hits[0].MatchKind);
    }

    [TestMethod]
    public async Task SearchActiveToolCalls_ReadsOnlyActiveHarnessCalls()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection, 11);
        await db.Database.EnsureCreatedAsync();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var sessions = CreateSessionService(db, provider);
        var messages = CreateMessageService(db, provider);
        var trajectories = CreateTrajectoryService(db, provider);
        var visible = await sessions.CreateSessionAsync("可见会话", 12);
        var archived = await sessions.CreateSessionAsync("归档会话", 12);
        var visibleRun = await trajectories.CreateAsync(visible.Id, 12, "可见", AdminChatMode.Harness);
        await trajectories.AppendAsync(visibleRun, "tool.call", "tool", "workspace_list", null, null);
        await trajectories.AppendAsync(visibleRun, "tool.result", "tool", "workspace_list", "结果", null);
        var reply = await messages.AddMessageAsync(visible.Id, ChatMessageRoleType.Assistant, "已调用");
        reply.SetTrajectory(visibleRun.Id, visibleRun.LastSequence);
        var archivedRun = await trajectories.CreateAsync(archived.Id, 12, "归档", AdminChatMode.Harness);
        await trajectories.AppendAsync(archivedRun, "tool.call", "tool", "workspace_list", null, null);
        archived.Archive();
        await db.SaveChangesAsync();

        var hits = await messages.SearchActiveToolCallsAsync(12, "list");

        Assert.AreEqual(1, hits.Count);
        Assert.AreEqual(visible.Id, hits[0].SessionId);
        Assert.AreEqual(reply.Id, hits[0].MessageId);
        Assert.AreEqual("list", hits[0].SnippetMatch);
        Assert.AreEqual("workspace_", hits[0].SnippetBefore);
    }

    private static AdminChatSession Session(int id, int userId, string title)
    {
        var session = new AdminChatSession(title, userId);
        session.Id = id;
        return session;
    }

    private static AdminChatMessage Message(int id, int sessionId, string content)
    {
        var message = new AdminChatMessage(sessionId, ChatMessageRoleType.User, content, id);
        message.Id = id;
        return message;
    }

    private static AdminChatTrajectory Trajectory(int id, int sessionId, int userId)
    {
        var trajectory = new AdminChatTrajectory(sessionId, userId, "run", AdminChatMode.Harness);
        trajectory.Id = id;
        return trajectory;
    }

    private static AdminChatTrajectoryEvent Event(
        int trajectoryId, int sequence, string eventType, string name, string content = "")
    {
        return new AdminChatTrajectoryEvent(trajectoryId, sequence, eventType, "tool", name, content, "");
    }

    private static SearchContext CreateContext(SqliteConnection connection, int tenantId)
    {
        EntitySetKeys.TryLoadSetInfo(typeof(SearchContext));
        return new SearchContext(new DbContextOptionsBuilder().UseSqlite(connection).Options, tenantId);
    }

    private static AdminChatSessionService CreateSessionService(SearchContext db, IServiceProvider provider) =>
        new(new AdminChatSessionRepository(new SearchDbData(db)), provider);

    private static AdminChatMessageService CreateMessageService(SearchContext db, IServiceProvider provider) =>
        new(new AdminChatMessageRepository(new SearchDbData(db)), provider);

    private static AdminChatTrajectoryService CreateTrajectoryService(SearchContext db, IServiceProvider provider) =>
        new(new AdminChatTrajectoryRepository(new SearchDbData(db)),
            new AdminChatTrajectoryEventService(new AdminChatTrajectoryEventRepository(new SearchDbData(db)), provider),
            provider);

    private sealed class SearchContext : SenparcEntitiesDbContextBase
    {
        public DbSet<AdminChatSession> Sessions => Set<AdminChatSession>();
        public DbSet<AdminChatMessage> Messages => Set<AdminChatMessage>();
        public DbSet<AdminChatTrajectory> Trajectories => Set<AdminChatTrajectory>();
        public DbSet<AdminChatTrajectoryEvent> TrajectoryEvents => Set<AdminChatTrajectoryEvent>();

        public SearchContext(DbContextOptions options, int tenantId) : base(options, null!)
        {
            SetMultiTenantEnable(true);
            TenantInfo = new RequestTenantInfo { Id = tenantId };
            TenantInfo.TryMatch(true);
        }
    }

    private sealed class SearchDbData(DbContext context) : NcfDbData
    {
        public override DbContext BaseDataContext => context;
        public override void CloseConnection() { }
    }
}
