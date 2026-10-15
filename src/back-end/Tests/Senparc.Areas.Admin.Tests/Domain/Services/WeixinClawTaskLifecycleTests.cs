using Senparc.Xncf.WeixinManager.WeixinClaw;
using System.Reflection;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class WeixinClawTaskLifecycleTests
{
    [TestMethod]
    public async Task RunningAccount_ConcurrentCancellationAndDisposalAreSafe()
    {
        for (var iteration = 0; iteration < 100; iteration++)
        {
            var (state, cancel, _) = CreateState();
            await Task.WhenAll(
                Task.Run(() => { for (var i = 0; i < 20; i++) cancel(); }),
                Task.Run(() => { for (var i = 0; i < 20; i++) state.Dispose(); }),
                Task.Run(cancel));
            cancel();
            state.Dispose();
        }
    }

    [TestMethod]
    public void RunningAccount_DisposalFromCancellationCallbackIsDeferred()
    {
        var (state, cancel, token) = CreateState();
        using var registration = token.Register(state.Dispose);
        cancel();
        Assert.IsTrue(token.IsCancellationRequested);
        cancel();
        state.Dispose();
    }

    [TestMethod]
    public void RunningAccount_LinksHostShutdownAndCanBeDisposedTwice()
    {
        using var stopping = new CancellationTokenSource();
        var (state, cancel, token) = CreateState(stopping.Token);
        stopping.Cancel();
        Assert.IsTrue(token.IsCancellationRequested);
        state.Dispose();
        state.Dispose();
        cancel();
    }

    [TestMethod]
    public async Task RunningAccount_HostShutdownAndCallbackDisposalCannotDeadlock()
    {
        for (var iteration = 0; iteration < 100; iteration++)
        {
            using var stopping = new CancellationTokenSource();
            var (state, cancel, token) = CreateState(stopping.Token);
            using var registration = token.Register(state.Dispose);
            await Task.WhenAll(Task.Run(stopping.Cancel), Task.Run(state.Dispose), Task.Run(cancel))
                .WaitAsync(TimeSpan.FromSeconds(5));
            state.Dispose();
            cancel();
        }
    }

    private static (IDisposable State, Action Cancel, CancellationToken Token) CreateState(
        CancellationToken stoppingToken = default)
    {
        var type = typeof(WeixinClawHostedService).GetNestedType("RunningAccount", BindingFlags.NonPublic)!;
        var state = (IDisposable)Activator.CreateInstance(type, stoppingToken)!;
        var cancel = (Action)type.GetMethod("Cancel")!.CreateDelegate(typeof(Action), state);
        var token = (CancellationToken)type.GetProperty("Token")!.GetValue(state)!;
        return (state, cancel, token);
    }
}
