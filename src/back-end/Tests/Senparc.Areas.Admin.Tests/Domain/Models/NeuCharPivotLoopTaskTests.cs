/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotLoopTaskTests.cs
    文件功能描述：NeuCharPivot Loop Task 调度状态测试
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Models;

[TestClass]
public class NeuCharPivotLoopTaskTests
{
    [TestMethod]
    public void Configure_ShouldClampIntervalAndScheduleOnlyWhenEnabled()
    {
        var task = new NeuCharPivotLoopTask(1, 2);

        task.Configure(1, "{}", true, true);

        Assert.AreEqual(60, task.IntervalSeconds);
        Assert.IsTrue(task.Enabled);
        Assert.IsTrue(task.UseNeuBell);
        Assert.IsNotNull(task.NextRunAt);

        task.Configure(120, "{}", false, false);

        Assert.IsFalse(task.Enabled);
        Assert.IsNull(task.NextRunAt);
        Assert.AreEqual(0, task.ConsecutiveFailures);
    }

    [TestMethod]
    public void DisableForError_ShouldStopMissingFunctionFromRepeating()
    {
        var task = new NeuCharPivotLoopTask(1, 2);
        task.Configure(60, "{}", true, true);

        task.DisableForError(new string('x', 5000));

        Assert.IsFalse(task.Enabled);
        Assert.IsNull(task.NextRunAt);
        Assert.AreEqual(false, task.LastSucceeded);
        Assert.AreEqual(1, task.ConsecutiveFailures);
        Assert.AreEqual(4000, task.LastError.Length);
    }

    [TestMethod]
    public void NeuBellProvider_ConsumeAll_ShouldAcknowledgeVisibleLoopResultsOnce()
    {
        var provider = new NeuCharPivotNeuBellProvider();
        provider.AddResult(1, "示例 Function", true, DateTimeOffset.UtcNow);
        provider.AddResult(2, "失败 Function", false, DateTimeOffset.UtcNow);

        Assert.AreEqual(2, provider.ConsumeAll());
        Assert.AreEqual(0, provider.ConsumeAll());
    }
}
