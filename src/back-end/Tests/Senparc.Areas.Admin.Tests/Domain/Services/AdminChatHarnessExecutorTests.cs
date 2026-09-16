using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatHarnessExecutorTests
{
    private static AdminChatHarnessExecutor NewExecutor(string[] outputs, int maxSteps = 8, TimeSpan? timeout = null)
    {
        var i = 0;
        return new AdminChatHarnessExecutor
        {
            MaxSteps = maxSteps,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            TurnExecutor = (step, prompt, ct) => Task.FromResult(i < outputs.Length ? outputs[i++] : string.Empty)
        };
    }

    [TestMethod]
    public async Task RunAsync_DoneOnFirstStep_ShouldCompleteInOneStep()
    {
        var executor = NewExecutor(new[] { "最终答案 " + AdminChatHarnessExecutor.DoneToken });
        var result = await executor.RunAsync("任务", "继续");

        Assert.AreEqual(1, result.StepsUsed);
        Assert.IsTrue(result.Completed);
        Assert.IsFalse(result.ReachedMaxSteps);
        Assert.AreEqual(1, result.Steps.Count);
        Assert.AreEqual("最终答案", result.FinalText);
    }

    [TestMethod]
    public async Task RunAsync_ContinueThenDone_ShouldStopOnDone()
    {
        var executor = NewExecutor(new[]
        {
            "第一步：查询数据 " + AdminChatHarnessExecutor.ContinueToken,
            "第二步：整理结果 " + AdminChatHarnessExecutor.DoneToken
        });
        var result = await executor.RunAsync("任务", "继续");

        Assert.AreEqual(2, result.StepsUsed);
        Assert.IsTrue(result.Completed);
        Assert.AreEqual(2, result.Steps.Count);
        Assert.IsFalse(result.Steps[0].IsFinal);
        Assert.IsTrue(result.Steps[1].IsFinal);
        Assert.AreEqual("第二步：整理结果", result.FinalText);
    }

    [TestMethod]
    public async Task RunAsync_ReachMaxSteps_ShouldMarkReachedMaxSteps()
    {
        var executor = NewExecutor(new[]
        {
            "a " + AdminChatHarnessExecutor.ContinueToken,
            "b " + AdminChatHarnessExecutor.ContinueToken,
            "c " + AdminChatHarnessExecutor.ContinueToken
        }, maxSteps: 3);
        var result = await executor.RunAsync("任务", "继续");

        Assert.AreEqual(3, result.StepsUsed);
        Assert.IsFalse(result.Completed);
        Assert.IsTrue(result.ReachedMaxSteps);
        Assert.AreEqual(3, result.Steps.Count);
    }

    [TestMethod]
    public async Task RunAsync_ShouldStripMarkersFromOutput()
    {
        var executor = NewExecutor(new[] { "内容一\n内容二 " + AdminChatHarnessExecutor.DoneToken });
        var result = await executor.RunAsync("任务", "继续");

        Assert.IsFalse(Regex.IsMatch(result.FinalText, @"\[\["));
        Assert.IsTrue(result.FinalText.Contains("内容一"));
        Assert.IsTrue(result.FinalText.Contains("内容二"));
    }

    [TestMethod]
    public async Task RunAsync_ShouldRecordStepsViaCallback()
    {
        var seen = new List<AdminChatHarnessStep>();
        var executor = NewExecutor(new[] { "s1 " + AdminChatHarnessExecutor.ContinueToken, "s2 " + AdminChatHarnessExecutor.DoneToken });
        await executor.RunAsync("任务", "继续", onStep: seen.Add);

        Assert.AreEqual(2, seen.Count);
        Assert.AreEqual(1, seen[0].Index);
        Assert.AreEqual(2, seen[1].Index);
    }

    [TestMethod]
    public async Task RunAsync_Cancellation_ShouldStopEarly()
    {
        using var cts = new CancellationTokenSource();
        var executor = new AdminChatHarnessExecutor
        {
            MaxSteps = 10,
            Timeout = TimeSpan.FromSeconds(30),
            TurnExecutor = (step, prompt, ct) =>
            {
                if (step == 2)
                {
                    cts.Cancel();
                }
                return Task.FromResult(step == 1 ? "s1 " + AdminChatHarnessExecutor.ContinueToken : "s2 " + AdminChatHarnessExecutor.ContinueToken);
            }
        };

        var result = await executor.RunAsync("任务", "继续", cancellationToken: cts.Token);

        Assert.IsFalse(result.Completed);
        Assert.IsTrue(result.StepsUsed < 10);
    }

    [TestMethod]
    public void StripMarkers_ShouldRemoveTokensAndCollapseWhitespace()
    {
        var cleaned = AdminChatHarnessExecutor.StripMarkers("line1\n\n\n\nline2 " + AdminChatHarnessExecutor.ContinueToken + " end " + AdminChatHarnessExecutor.DoneToken);
        Assert.IsFalse(Regex.IsMatch(cleaned, @"\[\[(CONTINUE|DONE)\]\]", RegexOptions.IgnoreCase));
        Assert.IsTrue(cleaned.Contains("line1"));
        Assert.IsTrue(cleaned.Contains("line2"));
    }

    [TestMethod]
    public void ContainsToken_ShouldBeCaseInsensitive()
    {
        Assert.IsTrue(AdminChatHarnessExecutor.ContainsToken("ok [[done]]", AdminChatHarnessExecutor.DoneToken));
        Assert.IsFalse(AdminChatHarnessExecutor.ContainsToken("ok", AdminChatHarnessExecutor.DoneToken));
        Assert.IsFalse(AdminChatHarnessExecutor.ContainsToken(null, AdminChatHarnessExecutor.DoneToken));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void RunAsync_NullTurnExecutor_ShouldThrow()
    {
        var executor = new AdminChatHarnessExecutor { MaxSteps = 1 };
        executor.RunAsync("任务", "继续").GetAwaiter().GetResult();
    }
}
