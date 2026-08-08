/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellTestProviderTests.cs
    文件功能描述：纽铃可见提醒示例 Provider 测试

    创建标识：Senparc - 20260805

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuBellTestProviderTests
{
    [TestMethod]
    public async Task SendAndConsumeAll_ShouldUpdateReminderSnapshot()
    {
        var provider = new NeuBellTestProvider();
        var context = new NeuBellRequestContext("admin");

        var emptySnapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(0, emptySnapshot.Items.Count);

        Assert.AreEqual(1, provider.Send());
        Assert.AreEqual(2, provider.Send());

        var pendingSnapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(NeuBellTestProvider.ProviderIdValue, pendingSnapshot.ProviderId);
        Assert.AreEqual(Register.ModuleUid, pendingSnapshot.ModuleUid);
        Assert.AreEqual(1, pendingSnapshot.Items.Count);
        Assert.AreEqual(2, pendingSnapshot.Items[0].Count);
        Assert.AreEqual("warning", pendingSnapshot.Items[0].Severity);

        Assert.AreEqual(2, provider.ConsumeAll());

        var consumedSnapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(0, consumedSnapshot.Items.Count);
        Assert.AreEqual(0, provider.ConsumeAll());
    }
}
