using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Abstractions.Memory;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatMemoryAuthorizationTests : TestBase
{
    [DataTestMethod]
    [DataRow("请记录")]
    [DataRow("立即保存")]
    [DataRow("请你使用 “审查候选记忆”功能 进行保存")]
    [DataRow("Please save it now.")]
    [DataRow("Record that.")]
    [DataRow("Note it down.")]
    public async Task MultiTurn_AnnouncementThenUserFactThenSave_PersistsOnlyOnExplicitRequest(string saveRequest)
    {
        var root = Path.Combine(Path.GetTempPath(), "ncf-memory-turns-" + Guid.NewGuid().ToString("N"));
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ContentRootPath).Returns(root);
        var store = new AdminChatMemoryStore(environment.Object, new EphemeralDataProtectionProvider());
        var sessions = _serviceProvider.GetRequiredService<AdminChatSessionService>();
        var messages = _serviceProvider.GetRequiredService<AdminChatMessageService>();
        var prompt = new Mock<IMemoryPromptProvider>();
        prompt.Setup(item => item.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryUpdatePrompt("test", "test-T1-A1"));
        var provider = new Mock<IServiceProvider>();
        provider.Setup(value => value.GetService(It.IsAny<Type>())).Returns((Type type) =>
            type == typeof(IMemoryPromptProvider) ? prompt.Object : _serviceProvider.GetService(type));
        var service = new AdminChatMemoryService(store, sessions, provider.Object, NullLogger<AdminChatMemoryService>.Instance);
        try
        {
            var session = await sessions.CreateSessionAsync("Synthetic multi-turn Memory", 12);
            var initial = await service.GetAsync(12, session.Id);
            var mode = await service.SetModeAsync(12, session.Id, initial.Revision, AdminChatMemoryMode.Learn);
            const string announcement = "请你关注接下去我提供的一些列关键信息，完成后请你记住。";
            await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, announcement);
            var announced = await service.BeginAsync(12, session.Id, announcement, default);
            await service.CompleteAsync(announced, announcement, 0,
                (_, _, _) => Task.FromResult("""{"intent":"defer","changes":[]}"""), default);
            Assert.AreEqual("intent-deferred", service.UpdateResult.Reason);
            await messages.AddMessageAsync(session.Id, ChatMessageRoleType.Assistant, "Please provide the information.");
            const string fact = "测试角色乙也叫Beta。";
            var source = await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, fact);
            var stated = await service.BeginAsync(12, session.Id, fact, default);
            await service.CompleteAsync(stated, fact, 0,
                (_, _, _) => throw new AssertFailedException("An announcement does not authorize immediate storage of the next fact."), default);
            Assert.AreEqual("skipped-input", service.UpdateStatus);
            Assert.AreEqual(mode.Revision, (await service.GetAsync(12, session.Id)).Revision);
            await messages.AddMessageAsync(session.Id, ChatMessageRoleType.Assistant, "Acknowledged, not a save receipt.");
            var request = await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, saveRequest);
            var requested = await service.BeginAsync(12, session.Id, saveRequest, default);
            Assert.AreEqual(request.Id, requested.SourceMessageId);
            Assert.IsTrue(requested.RecentMessages.Any(item => item.MessageId == source.Id && item.Role == "user"));
            await messages.AddMessageAsync(session.Id, ChatMessageRoleType.Assistant, "Later assistant text must not become evidence.");
            var output = JsonSerializer.Serialize(new { intent = "update", changes = new[] { new {
                key = "fictional-beta", text = fact, topic = "Beta", tier = "topic",
                kind = "reference", scope = "global", evidence = fact, remove = false
            }} });
            await service.CompleteAsync(requested, saveRequest, 0, (instructions, input, _) =>
            {
                StringAssert.Contains(instructions, "Quote the concrete fact as evidence");
                using var json = JsonDocument.Parse(input);
                var context = json.RootElement.GetProperty("recentMessages").EnumerateArray();
                Assert.IsTrue(context.Any(item => item.GetProperty("MessageId").GetInt32() == source.Id));
                Assert.IsFalse(input.Contains("Later assistant text"));
                return Task.FromResult(output);
            }, default);
            Assert.AreEqual("saved", service.UpdateResult.Reason);
            Assert.AreEqual(1, service.UpdateResult.GlobalChanges);
            Assert.AreEqual(0, service.UpdateResult.Proposals.Count);
            var saved = await service.GetAsync(12, session.Id);
            Assert.AreEqual(fact, saved.Entries.Single().Text);
            Assert.AreEqual(source.Id, saved.Entries.Single().SourceMessageId, "Track the fact's source, not the short save command.");
            Assert.AreEqual("user-message", saved.Entries.Single().Source);
            Assert.IsFalse(saved.Entries.Single().Pinned);
            StringAssert.Contains((await service.BeginAsync(12, session.Id, "Beta", default)).Instructions, fact);
            await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, "立即保存");
            var repeated = await service.BeginAsync(12, session.Id, "立即保存", default);
            await service.CompleteAsync(repeated, "立即保存", 0, (_, _, _) => Task.FromResult(output), default);
            Assert.AreEqual("no-difference", service.UpdateResult.Reason);
            Assert.AreEqual(saved.Revision, (await service.GetAsync(12, session.Id)).Revision);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task ModesAndEntryEditing_EnforceUserSessionAndWorkspaceBoundaries()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncf-memory-auth-" + Guid.NewGuid().ToString("N"));
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ContentRootPath).Returns(root);
        environment.SetupGet(value => value.WebRootPath).Returns(Path.Combine(root, "wwwroot"));
        var store = new AdminChatMemoryStore(environment.Object, new EphemeralDataProtectionProvider());
        var sessions = _serviceProvider.GetRequiredService<AdminChatSessionService>();
        var provider = new Mock<IServiceProvider>();
        provider.Setup(value => value.GetService(It.IsAny<Type>())).Returns((Type type) =>
        {
            if (type == typeof(IMemoryPromptProvider)) throw new AssertFailedException("Read-only initialized PromptRange.");
            return _serviceProvider.GetService(type);
        });
        var service = new AdminChatMemoryService(store, sessions, provider.Object, NullLogger<AdminChatMemoryService>.Instance);
        try
        {
            var alpha = await sessions.CreateSessionAsync("Memory alpha", 12, "alpha");
            var beta = await sessions.CreateSessionAsync("Memory beta", 12, "beta");
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.GetAsync(13, alpha.Id));
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.GetAsync(0, 0));
            var initial = await service.GetAsync(12, alpha.Id);
            Assert.AreEqual(AdminChatMemoryMode.Off, initial.Mode);
            var readOnly = await service.SetModeAsync(12, alpha.Id, initial.Revision, AdminChatMemoryMode.ReadOnly);
            Assert.AreEqual(AdminChatMemoryMode.ReadOnly, readOnly.Mode);
            var edited = await service.EditAsync(12, alpha.Id, readOnly.Revision,
                new("", 0, "Prefer typed APIs", "typescript", "topic", "project", true, "workspace", null, false));
            Assert.AreEqual("alpha", edited.Entries.Single().WorkspaceId);
            Assert.AreEqual(0, (await service.GetAsync(12, beta.Id)).Entries.Count);
            Assert.AreEqual(0, (await service.GetAsync(12, 0)).Entries.Count);
            var entry = edited.Entries.Single();
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.EditAsync(12, beta.Id, edited.Revision,
                new(entry.Id, entry.Version, "cross-scope", "", "core", "preference", true, "global", null, false)));
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.EditAsync(12, alpha.Id, edited.Revision,
                new(entry.Id, entry.Version + 1, "stale", "", "core", "preference", true, "global", null, false)));
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.EditAsync(12, 0, edited.Revision,
                new("", 0, "unbound", "", "core", "project", true, "workspace", null, false)));
            var beforeRead = edited.Revision;
            var context = await service.BeginAsync(12, alpha.Id, "typescript", default, "gpt-4o");
            StringAssert.Contains(context.Instructions, "Prefer typed APIs");
            Assert.AreEqual(AdminChatMemoryMode.ReadOnly, context.Mode);
            await service.CompleteAsync(context, "Remember: prefer JavaScript", 0,
                (_, _, _) => throw new AssertFailedException("Read-only called model."), default);
            Assert.AreEqual(beforeRead, (await service.GetAsync(12, alpha.Id)).Revision);
            var off = await service.SetModeAsync(12, alpha.Id, beforeRead, AdminChatMemoryMode.Off);
            var offContext = await service.BeginAsync(12, alpha.Id, "typescript", default, "gpt-4o");
            Assert.IsFalse(offContext.Instructions.Contains("Prefer typed APIs"));
            Assert.AreEqual(0, service.Usage.Entries.Count);
            Assert.AreEqual(AdminChatMemoryMode.Off, off.Mode);
            var messages = _serviceProvider.GetRequiredService<AdminChatMessageService>();
            await messages.AddMessageAsync(alpha.Id, ChatMessageRoleType.User, "Remember: prefer typed APIs");
            var latest = await messages.AddMessageAsync(alpha.Id, ChatMessageRoleType.User, "Remember: prefer typed APIs");
            await store.UpdateAsync(0, 12, off.Revision, profile => profile.SessionModes[alpha.Id] = AdminChatMemoryMode.Learn);
            var learnContext = await service.BeginAsync(12, alpha.Id, "Remember: prefer typed APIs", default, "gpt-4o");
            Assert.AreEqual(latest.Id, learnContext.SourceMessageId, "Evidence must link to the current message, not an earlier duplicate.");
            Assert.AreEqual(1, learnContext.RecentMessages.Count, "Eligible requests get bounded context even without a literal reference word.");
            const string fact = "测试角色乙的昵称是Beta。";
            var source = await messages.AddMessageAsync(alpha.Id, ChatMessageRoleType.Assistant, fact);
            await messages.AddMessageAsync(beta.Id, ChatMessageRoleType.User, "Other session must never enter extraction.");
            await messages.AddMessageAsync(alpha.Id, ChatMessageRoleType.System, "System message must never enter extraction.");
            var request = await messages.AddMessageAsync(alpha.Id, ChatMessageRoleType.User, "记住上述内容");
            var referenced = await service.BeginAsync(12, alpha.Id, "记住上述内容", default);
            Assert.AreEqual(request.Id, referenced.SourceMessageId);
            Assert.IsTrue(referenced.RecentMessages.Any(item => item.MessageId == source.Id && item.Role == "assistant"));
            Assert.IsFalse(referenced.RecentMessages.Any(item => item.Text.Contains("must never")));
            await messages.AddMessageAsync(alpha.Id, ChatMessageRoleType.Assistant, "Later answer is not prior context");
            Assert.IsFalse(referenced.RecentMessages.Any(item => item.Text.Contains("Later answer")));
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => service.BeginAsync(13, alpha.Id, "记住上述内容", default));

            var prompt = new Mock<IMemoryPromptProvider>();
            prompt.Setup(item => item.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryUpdatePrompt("test", "test-T1-A1"));
            using var reviewServices = new ServiceCollection().AddSingleton(prompt.Object).BuildServiceProvider();
            var reviewService = new AdminChatMemoryService(store, sessions, reviewServices, NullLogger<AdminChatMemoryService>.Instance);
            var output = System.Text.Json.JsonSerializer.Serialize(new { intent = "update", changes = new[] { new {
                key = "fictional-beta", text = fact, topic = "Beta", tier = "topic",
                kind = "reference", scope = "global", evidence = fact, remove = false
            }} });
            await reviewService.CompleteAsync(referenced, "记住上述内容", 0, (_, _, _) => Task.FromResult(output), default);
            Assert.AreEqual("pending-review", reviewService.UpdateStatus);
            var candidate = reviewService.UpdateResult.Proposals.Single().Entry;
            var change = new AdminChatMemoryEdit(candidate.Id, candidate.Version, candidate.Text, candidate.Topic,
                candidate.Tier, candidate.Kind, candidate.Pinned, "global", candidate.ExpiresAt, false);
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() =>
                service.EditAsync(13, alpha.Id, reviewService.UpdateResult.Revision, change));
            var confirmed = await service.EditAsync(12, alpha.Id, reviewService.UpdateResult.Revision, change);
            Assert.IsTrue(confirmed.Entries.Any(item => item.Text == fact && item.Pinned && item.Source == "manual"));
            await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() =>
                service.EditAsync(12, alpha.Id, reviewService.UpdateResult.Revision, change));
            StringAssert.Contains((await service.BeginAsync(12, alpha.Id, "Beta", default)).Instructions, fact);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
