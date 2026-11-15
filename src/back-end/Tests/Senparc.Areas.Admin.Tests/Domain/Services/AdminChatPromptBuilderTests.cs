using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatPromptBuilderTests
{
    [TestMethod]
    public void BuildUserPrompt_BoundsHistoryAndCurrentMessage()
    {
        var messages = new[]
        {
            new AdminChatMessage(1, ChatMessageRoleType.User, new string('h', 10_000), 1),
            new AdminChatMessage(1, ChatMessageRoleType.Assistant, new string('a', 10_000), 2)
        };

        var prompt = AdminChatPromptBuilder.BuildUserPrompt(messages, new string('c', 10_000));

        Assert.IsTrue(prompt.Length <= AdminChatPromptBuilder.MaxUserPromptCharacters);
        StringAssert.Contains(prompt, "[较早对话已省略]");
        StringAssert.Contains(prompt, "[内容已截断]");
        StringAssert.Contains(prompt, new string('c', 100));
    }

    [TestMethod]
    public void BuildUserPrompt_UsesOnlyLatestTwelveHistoryMessages()
    {
        var messages = Enumerable.Range(1, 13)
            .Select(sequence => new AdminChatMessage(1, ChatMessageRoleType.User, $"message-{sequence}", sequence))
            .ToArray();

        var prompt = AdminChatPromptBuilder.BuildUserPrompt(messages, "current");

        Assert.IsFalse(prompt.Contains("message-1\n", StringComparison.Ordinal));
        StringAssert.Contains(prompt, "message-2");
        StringAssert.Contains(prompt, "message-13");
        StringAssert.Contains(prompt, "[用户当前问题] current");
    }
}
