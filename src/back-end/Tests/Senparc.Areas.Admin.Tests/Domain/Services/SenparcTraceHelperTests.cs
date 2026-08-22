/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SenparcTraceHelperTests.cs
    文件功能描述：SenparcTrace 轻量日志扫描测试

    创建标识：Senparc - 20260809

----------------------------------------------------------------*/

using Senparc.Areas.Admin.SenparcTraceManager;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class SenparcTraceHelperTests
{
    [TestMethod]
    public async Task GetLogScanResultAsync_ShouldCountWithoutLoadingBodies()
    {
        var originalPath = SenparcTraceHelper.DefaultLogPath;
        var tempDir = Path.Combine(Path.GetTempPath(), "ncf-trace-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            SenparcTraceHelper.DefaultLogPath = tempDir;
            const string date = "20260809";
            var logPath = Path.Combine(tempDir, $"SenparcTrace-{date}.log");

            await File.WriteAllTextAsync(logPath, """
[[[ScanAssambles]]]
[2026/08/09 00:28:36.3626]
[线程：1]
RegisterAllAreas 用时：305.891ms

[[[系统启动]]]
[2026/08/09 00:28:36.3898]
[线程：1]
完成 Area:Admin 注册

[[[BaseException]]]
[2026/08/09 00:28:37.0674]
[线程：1]
	BaseException
	Message：demo

[[[API]]]
[2026/08/09 01:00:00.0000]
[线程：2]
URL：https://example.com/api
Result：
{"ok":true}

[[[Post]]]
[2026/08/09 01:01:00.0000]
[线程：3]
URL：https://example.com/post
Post Data：
{"a":1}

""");

            // 一边追加写入，一边共享读（验证 FileShare.ReadWrite）
            await using (var writer = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            await using (var sw = new StreamWriter(writer))
            {
                await sw.WriteLineAsync("[[[RuntimeAppend]]]");
                await sw.WriteLineAsync("[2026/08/09 01:02:00.0000]");
                await sw.FlushAsync();

                var scan = await SenparcTraceHelper.GetLogScanResultAsync(serviceProvider: null!, date, useCache: false);

                Assert.AreEqual(6, scan.TotalLogCount);
                Assert.AreEqual(1, scan.ExceptionLogCount);
                Assert.AreEqual(5, scan.NormalLogCount);
                Assert.AreEqual(1, scan.TypeCounts[SenparcTraceType.Exception]);
                Assert.AreEqual(1, scan.TypeCounts[SenparcTraceType.GetRequest]);
                Assert.AreEqual(1, scan.TypeCounts[SenparcTraceType.PostRequest]);
                Assert.IsTrue(scan.TypeCounts[SenparcTraceType.Normal] >= 3);
            }
        }
        finally
        {
            SenparcTraceHelper.DefaultLogPath = originalPath;
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // ignore cleanup
            }
        }
    }
}
