using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Abstractions.Memory;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatMemoryAuthorizationTests : TestBase
{
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
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
