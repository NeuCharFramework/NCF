/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellTestProviderTests.cs
    文件功能描述：纽铃可见提醒示例 Provider 测试

    创建标识：Senparc - 20260805

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Ncf.XncfBase;
using System.Reflection;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuBellTestProviderTests
{
    [TestMethod]
    public async Task SendConsumeOneAndConsumeAll_ShouldUpdateReminderSnapshot()
    {
        var provider = new NeuBellTestProvider();
        var context = new NeuBellRequestContext("admin");
        var consumable = (INeuBellConsumableProvider)provider;

        var emptySnapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(0, emptySnapshot.Items.Count);

        Assert.AreEqual(1, provider.Send());
        Assert.AreEqual(2, provider.Send());

        var pendingSnapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(NeuBellTestProvider.ProviderIdValue, pendingSnapshot.ProviderId);
        Assert.AreEqual(Register.ModuleUid, pendingSnapshot.ModuleUid);
        Assert.AreEqual(2, pendingSnapshot.Items.Count);
        Assert.IsTrue(pendingSnapshot.Items.All(item => item.Count == 1 && item.Severity == "warning"));

        Assert.AreEqual(1, await consumable.ConsumeItemAsync(context, pendingSnapshot.Items[0].Id));
        var afterOneConsumed = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(1, afterOneConsumed.Items.Count);
        Assert.AreEqual(0, await consumable.ConsumeItemAsync(context, "missing-item"));

        Assert.AreEqual(1, await consumable.ConsumeAllAsync(context));

        var consumedSnapshot = await provider.GetSnapshotAsync(context);
        Assert.AreEqual(0, consumedSnapshot.Items.Count);
        Assert.AreEqual(0, provider.ConsumeAll());
    }

    [TestMethod]
    public void TestFunctionOptions_ShouldExposeSingleAndSubscriptionConsumption()
    {
        var request = new Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request();
        var values = request.ActionOptions.Items.Select(item => item.Value).ToArray();

        CollectionAssert.Contains(values, Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request.SendAction);
        CollectionAssert.Contains(values, Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request.ConsumeOneAction);
        CollectionAssert.Contains(values, Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request.ConsumeAllAction);
    }

    [TestMethod]
    public void TestFunctionWebHookParameters_ShouldExposeDropdownMultilineInputAndDescriptions()
    {
        var requestType = typeof(Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request);
        var methodProperty = requestType.GetProperty(nameof(Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request.WebHookMethod));
        var bodyProperty = requestType.GetProperty(nameof(Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request.WebHookBody));

        var methodUi = methodProperty.GetCustomAttribute<FunctionParameterUiAttribute>();
        var bodyUi = bodyProperty.GetCustomAttribute<FunctionParameterUiAttribute>();
        var methodDescription = methodProperty.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
        var bodyDescription = bodyProperty.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

        Assert.IsNotNull(methodUi);
        Assert.AreEqual(ParameterType.DropDownList, methodUi.ParameterType);
        Assert.AreEqual(nameof(Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request.WebHookMethodOptions),
            methodUi.SelectionListPropertyName);
        Assert.IsNotNull(bodyUi);
        Assert.AreEqual(ParameterType.TextArea, bodyUi.ParameterType);
        StringAssert.Contains(methodDescription, "||");
        StringAssert.Contains(bodyDescription, "{{operationStatus}}");
        StringAssert.Contains(bodyDescription, "{{payload}}");

        var request = new Senparc.Areas.Admin.OHS.PL.NeuBellTest_Request();
        CollectionAssert.AreEqual(
            new[] { "POST", "GET", "PUT" },
            request.WebHookMethodOptions.Items.Select(item => item.Value).ToArray());
    }

    [TestMethod]
    public void ConsumeHelpers_ShouldReturnRemovedItemsForWebhookPayloads()
    {
        var provider = new NeuBellTestProvider();
        provider.Send();
        provider.Send();

        var latest = provider.ConsumeLatestAndGetItem();
        Assert.IsNotNull(latest);
        Assert.AreEqual(1, provider.PendingCount);

        var remaining = provider.ConsumeAllAndGetItems();
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual(0, provider.PendingCount);
        Assert.AreNotEqual(latest.Id, remaining[0].Id);
    }
}
