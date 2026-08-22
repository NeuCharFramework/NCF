using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuCharWorkflowNeuBellProviderTests
{
    [TestMethod]
    public async Task Send_ShouldLinkToTaskAndConsumeTheConfiguredSingleItem()
    {
        var provider = new NeuCharWorkflowNeuBellProvider();
        var context = new NeuBellRequestContext("42");
        var runId = Guid.Parse("f6d7e0a2-4f33-46f8-a9e3-116a272bab58");

        var notificationId = provider.Send(
            42,
            17,
            "发送提醒工作流",
            runId.ToString("N"),
            "notify",
            "处理完成",
            "请查看当前任务。",
            NeuCharWorkflowNeuBellConsumption.Item);

        var snapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(NeuCharWorkflowNeuBellProvider.ProviderIdValue, snapshot.ProviderId);
        Assert.AreEqual(1, snapshot.Items.Count);
        Assert.AreEqual(notificationId, snapshot.Items[0].Id);
        StringAssert.Contains(snapshot.Items[0].DetailUrl, "/Admin/NeuCharWorkflow/Tasks?");
        StringAssert.Contains(snapshot.Items[0].DetailUrl, "workflowId=17");
        StringAssert.Contains(snapshot.Items[0].DetailUrl, "runId=f6d7e0a24f3346f8a9e3116a272bab58");
        StringAssert.Contains(snapshot.Items[0].DetailUrl, "neuBellConsume=item");

        var consumable = (INeuBellConsumableProvider)provider;
        Assert.AreEqual(1, await consumable.ConsumeItemAsync(context, notificationId));
        Assert.AreEqual(0, (await provider.GetSnapshotAsync(context)).Items.Count);
    }

    [TestMethod]
    public async Task ConsumeAll_ShouldOnlyRemoveCurrentUsersSubscriptionItems()
    {
        var provider = new NeuCharWorkflowNeuBellProvider();
        var user42 = new NeuBellRequestContext("42");
        var user43 = new NeuBellRequestContext("43");
        var consumable = (INeuBellConsumableProvider)provider;

        provider.Send(42, 17, "工作流", null, "notify", "A", "A", NeuCharWorkflowNeuBellConsumption.Provider);
        provider.Send(42, 17, "工作流", null, "notify", "B", "B", NeuCharWorkflowNeuBellConsumption.Provider);
        provider.Send(43, 18, "另一工作流", null, "notify", "C", "C", NeuCharWorkflowNeuBellConsumption.None);

        Assert.AreEqual(2, await consumable.ConsumeAllAsync(user42));
        Assert.AreEqual(0, (await provider.GetSnapshotAsync(user42)).Items.Count);
        Assert.AreEqual(1, (await provider.GetSnapshotAsync(user43)).Items.Count);
    }
}
