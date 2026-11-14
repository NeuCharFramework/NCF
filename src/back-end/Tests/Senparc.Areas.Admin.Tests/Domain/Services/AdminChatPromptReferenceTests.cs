using Microsoft.Extensions.DependencyInjection;
using Moq;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Abstractions.References;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatPromptReferenceTests : TestBase
{
    private AdminChatMessageService CreateMessages(Mock<IPromptReferenceService>? references)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(service => service.GetService(It.IsAny<Type>()))
            .Returns((Type type) => type == typeof(IPromptReferenceService)
                ? references?.Object : _serviceProvider.GetService(type));
        return new AdminChatMessageService(
            _serviceProvider.GetRequiredService<IAdminChatMessageRepository>(), provider.Object);
    }

    [TestMethod]
    public async Task References_PersistSeparatelyAndOnlyOwnedCompletedTurnsCanBeScored()
    {
        var provider = new Mock<IPromptReferenceService>();
        provider.Setup(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<PromptReference>>()))
            .Returns(Task.CompletedTask);
        var expected = new PromptReferenceInfo(17, "2026.11.04.1-T1-A1", 8.2m, 9m, 8.2m, true);
        provider.Setup(service => service.ScoreWithAIAsync(17, "before PROMPT after", "Answer"))
            .ReturnsAsync(expected);
        var sessions = _serviceProvider.GetRequiredService<AdminChatSessionService>();
        var messages = CreateMessages(provider);
        var session = await sessions.CreateSessionAsync("References", 12);
        var references = new[] { new PromptReference { PromptItemId = 17, PromptCode = expected.PromptCode, Start = 7, Length = 6 } };
        var user = await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, "before PROMPT after",
            promptReferences: references);
        var dto = AdminChatMessageDto.CreateFromEntity(user);
        Assert.AreEqual("before PROMPT after", dto.Content);
        Assert.AreEqual(17, dto.PromptReferences.Single().PromptItemId);
        Assert.AreEqual(7, dto.PromptReferences.Single().Start);
        Assert.AreEqual("before PROMPT after", (await messages.GetSessionMessagesAsync(session.Id)).messages.Single().Content);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => messages.ScorePromptReferenceWithAIAsync(12, user.Id, 17));
        await messages.AddMessageAsync(session.Id, ChatMessageRoleType.Assistant, "Answer");
        Assert.AreEqual(expected, await messages.ScorePromptReferenceWithAIAsync(12, user.Id, 17));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => messages.ScorePromptReferenceWithAIAsync(13, user.Id, 17));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => messages.ScorePromptReferenceWithAIAsync(0, user.Id, 17));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => messages.ScorePromptReferenceWithAIAsync(12, user.Id, 18));
        var unanswered = await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, "before PROMPT after",
            promptReferences: references);
        await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, "Unrelated question");
        await messages.AddMessageAsync(session.Id, ChatMessageRoleType.Assistant, "Unrelated reply");
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => messages.ScorePromptReferenceWithAIAsync(12, unanswered.Id, 17));
        provider.Verify(service => service.ScoreWithAIAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        await messages.UpdateMessageContentAsync(user.Id, "edited");
        Assert.AreEqual(0, user.GetPromptReferences().Count);
        Assert.AreEqual("edited", user.Content);
    }

    [TestMethod]
    public async Task InvalidReferenceMustNotBecomeAPersistedMessage()
    {
        var provider = new Mock<IPromptReferenceService>();
        provider.Setup(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<PromptReference>>()))
            .ThrowsAsync(new NcfExceptionBase("Reference mismatch."));
        var sessions = _serviceProvider.GetRequiredService<AdminChatSessionService>();
        var messages = CreateMessages(provider);
        var session = await sessions.CreateSessionAsync("Invalid references", 12);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => messages.AddMessageAsync(
            session.Id, ChatMessageRoleType.User, "tampered", promptReferences:
            new[] { new PromptReference { PromptItemId = 17, PromptCode = "2026.11.04.1-T1-A1", Start = 0, Length = 8 } }));
        Assert.AreEqual(0, (await messages.GetSessionMessagesAsync(session.Id)).totalCount);
        var plain = await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, "plain text");
        Assert.AreEqual(0, plain.GetPromptReferences().Count);
    }

    [TestMethod]
    public void ReferenceMigrations_OnlyAddNullableMetadata()
    {
        var oracleMigration = new Senparc.Areas.Admin.Domain.Migrations.Oracle.Add_AdminChat_PromptReferences();
        Microsoft.EntityFrameworkCore.Migrations.Migration[] migrations =
        {
            new Senparc.Areas.Admin.Domain.Migrations.Sqlite.Add_AdminChat_PromptReferences(),
            new Senparc.Areas.Admin.Domain.Migrations.SqlServer.Add_AdminChat_PromptReferences(),
            new Senparc.Areas.Admin.Domain.Migrations.MySql.Add_AdminChat_PromptReferences(),
            new Senparc.Areas.Admin.Domain.Migrations.PostgreSQL.Add_AdminChat_PromptReferences(),
            oracleMigration,
            new Senparc.Areas.Admin.Domain.Migrations.Dm.Add_AdminChat_PromptReferences()
        };
        foreach (var migration in migrations)
        {
            Assert.AreEqual(1, migration.UpOperations.Count);
            var column = migration.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>().Single();
            Assert.AreEqual("PromptReferencesJson", column.Name);
            Assert.IsTrue(column.IsNullable);
            Assert.AreEqual(1, migration.DownOperations.Count);
        }
        var oracleColumn = oracleMigration.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>().Single();
        Assert.AreEqual("NCLOB", oracleColumn.ColumnType);
    }

    [TestMethod]
    public async Task MissingPromptRange_AllowsPlainMessagesButRejectsReferences()
    {
        var messages = CreateMessages(null);
        var sessions = _serviceProvider.GetRequiredService<AdminChatSessionService>();
        var session = await sessions.CreateSessionAsync("Without PromptRange", 12);
        var plain = await messages.AddMessageAsync(session.Id, ChatMessageRoleType.User, "plain");
        Assert.AreEqual("plain", plain.Content);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => messages.AddMessageAsync(
            session.Id, ChatMessageRoleType.User, "prompt", promptReferences:
            new[] { new PromptReference { PromptItemId = 17, PromptCode = "2026.11.04.1-T1-A1", Start = 0, Length = 6 } }));
        Assert.AreEqual(1, (await messages.GetSessionMessagesAsync(session.Id)).totalCount);
    }
}
