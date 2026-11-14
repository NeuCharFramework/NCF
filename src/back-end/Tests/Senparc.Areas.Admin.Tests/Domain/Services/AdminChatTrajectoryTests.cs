using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatTrajectoryTests
{
    [TestMethod]
    public void Trajectory_ShouldAdvanceAndPersistLifecycleState()
    {
        var trajectory = new AdminChatTrajectory(
            sessionId: 7,
            userId: 11,
            title: "部署任务",
            mode: AdminChatMode.Harness,
            modelIdentifier: "test-model");

        Assert.AreEqual(1, trajectory.NextSequence());
        trajectory.MarkSequence(3);
        trajectory.SetSessionState("{\"state\":true}");
        trajectory.WaitForApproval();

        Assert.AreEqual(3, trajectory.LastSequence);
        Assert.AreEqual(AdminChatTrajectoryStatus.WaitingForApproval, trajectory.Status);
        Assert.AreEqual("{\"state\":true}", trajectory.SessionStateJson);

        trajectory.Complete();
        Assert.AreEqual(AdminChatTrajectoryStatus.Completed, trajectory.Status);
        Assert.IsNotNull(trajectory.FinishedAt);
    }

    [TestMethod]
    public void Trajectory_ShouldKeepForkLineage()
    {
        var branch = new AdminChatTrajectory(
            sessionId: 7,
            userId: 11,
            title: "分叉任务",
            mode: AdminChatMode.Harness,
            parentTrajectoryId: 20,
            forkFromSequence: 8);

        Assert.AreEqual(20, branch.ParentTrajectoryId);
        Assert.AreEqual(8, branch.ForkFromSequence);
        Assert.AreEqual(AdminChatTrajectoryStatus.Running, branch.Status);
    }

    [TestMethod]
    public void AssistantMessage_ShouldKeepTrajectoryAssociation()
    {
        var message = new AdminChatMessage(
            sessionId: 7,
            roleType: ChatMessageRoleType.Assistant,
            content: "结果",
            sequence: 4,
            modelIdentifier: "test-model",
            trajectoryId: 20,
            trajectorySequence: 12);

        Assert.AreEqual(20, message.TrajectoryId);
        Assert.AreEqual(12, message.TrajectorySequence);
    }

    [TestMethod]
    public void CollapseAssistantTextChunks_ShouldMergeAdjacentAssistantAndReasoningChunks()
    {
        var events = new[]
        {
            new AdminChatTrajectoryEventDto
            {
                Id = 1,
                Sequence = 1,
                EventType = "assistant.text",
                Content = "正在",
                OccurredAt = DateTime.Parse("2026-09-15T10:00:00")
            },
            new AdminChatTrajectoryEventDto
            {
                Id = 2,
                Sequence = 2,
                EventType = "TextContent",
                Content = "TextContent",
                PayloadJson = "{\"Text\":\"TextContent\"}"
            },
            new AdminChatTrajectoryEventDto
            {
                Id = 3,
                Sequence = 3,
                EventType = "assistant.text",
                Content = "整理工具结果",
                OccurredAt = DateTime.Parse("2026-09-15T10:00:01")
            },
            new AdminChatTrajectoryEventDto
            {
                Id = 4,
                Sequence = 4,
                EventType = "tool.call",
                Name = "InspectDatabase"
            },
            new AdminChatTrajectoryEventDto
            {
                Id = 5,
                Sequence = 5,
                EventType = "TextContent",
                Content = "重复文本"
            },
            new AdminChatTrajectoryEventDto
            {
                Id = 6,
                Sequence = 6,
                EventType = "TextReasoningContent",
                PayloadJson = "{\"$type\":\"reasoning\",\"Text\":\"The\"}"
            },
            new AdminChatTrajectoryEventDto
            {
                Id = 7,
                Sequence = 7,
                EventType = "TextReasoningContent",
                PayloadJson = "{\"$type\":\"reasoning\",\"Text\":\" user\"}"
            }
        };

        var collapsed = AdminChatTrajectoryEventDto.CollapseAssistantTextChunks(events);

        Assert.AreEqual(3, collapsed.Count);
        Assert.AreEqual("正在整理工具结果", collapsed[0].Content);
        Assert.AreEqual("tool.call", collapsed[1].EventType);
        Assert.AreEqual("The user", collapsed[2].Content);
        Assert.AreEqual("{\"$type\":\"reasoning\",\"Text\":\" user\"}", collapsed[2].PayloadJson);
    }
}
