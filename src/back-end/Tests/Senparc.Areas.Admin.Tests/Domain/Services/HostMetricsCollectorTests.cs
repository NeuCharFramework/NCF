/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：HostMetricsCollectorTests.cs
    文件功能描述：Host 实时指标采集器测试

    创建标识：Senparc - 20260806

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class HostMetricsCollectorTests
{
    [TestMethod]
    public async Task Collect_ShouldReturnValidCurrentHostSnapshot()
    {
        var collector = new HostMetricsCollector();

        var first = collector.Collect();
        await Task.Delay(100);
        var second = collector.Collect();

        Assert.IsFalse(string.IsNullOrWhiteSpace(second.HostName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(second.OperatingSystem));
        Assert.IsTrue(second.SampledAt >= first.SampledAt);

        Assert.IsTrue(second.MemoryTotalBytes > 0);
        Assert.IsTrue(second.MemoryAvailableBytes >= 0);
        Assert.IsTrue(second.MemoryUsedBytes >= 0);
        Assert.AreEqual(second.MemoryTotalBytes, second.MemoryUsedBytes + second.MemoryAvailableBytes);
        AssertPercentage(second.MemoryUsagePercent);

        AssertPercentage(second.CpuUsagePercent);
        Assert.IsTrue(second.NetworkReceiveTotalBytes >= 0);
        Assert.IsTrue(second.NetworkSendTotalBytes >= 0);
        Assert.IsTrue(second.NetworkInterfaceCount >= 0);
        Assert.IsTrue(second.NetworkReceiveBytesPerSecond is null or >= 0);
        Assert.IsTrue(second.NetworkSendBytesPerSecond is null or >= 0);
        AssertPercentage(second.ProcessCpuUsagePercent);
        Assert.IsTrue(second.ProcessWorkingSetBytes > 0);
        Assert.IsTrue(second.ApplicationUptimeSeconds >= 0);
    }

    private static void AssertPercentage(double? value)
    {
        Assert.IsNotNull(value);
        Assert.IsTrue(value >= 0 && value <= 100);
    }
}
