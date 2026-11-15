using System.Threading;
using Microsoft.Extensions.Hosting;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatRunRegistryTests
{
    [TestMethod]
    public async Task DisconnectingAnObserverDoesNotCancelTheRun()
    {
        var registry = new AdminChatRunRegistry(new TestLifetime());
        var run = registry.TryBegin(3, 9);
        Assert.IsNotNull(run);
        Assert.IsNull(registry.TryBegin(3, 9), "同一会话同时只能有一个任务。");
        Assert.IsFalse(registry.Cancel(3, 8), "其他用户不能取消这个任务。");
        registry.Publish(run, "token", "{\"text\":\"hello\"}", state =>
        {
            state.AssistantText += "hello";
            state.StatusText = "正在生成回答…";
        });

        var listener = registry.Listen(run);
        StringAssert.Contains(listener.SnapshotJson, "hello");
        var read = listener.Channel.Reader.ReadAsync().AsTask();
        registry.Publish(run, "token", "{\"text\":\"!\"}", state => state.AssistantText += "!");
        Assert.AreEqual("token\n{\"text\":\"!\"}", await read);
        listener.Dispose();

        Assert.IsFalse(run!.Token.IsCancellationRequested, "观察者断开不能停止生成。");
        Assert.IsTrue(registry.Cancel(3, 9));
        Assert.IsTrue(run.Token.IsCancellationRequested);
        registry.Finish(run);

        var status = registry.GetStatus(3, 9);
        Assert.IsFalse(status.Running);
        Assert.IsTrue(status.JustFinished);
        Assert.IsFalse(registry.Cancel(3, 9), "结束后不能再次取消已经释放的任务。");
        var restarted = registry.TryBegin(3, 9);
        Assert.IsNotNull(restarted);
        registry.Finish(restarted);
    }

    private sealed class TestLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}
