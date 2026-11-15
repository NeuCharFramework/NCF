/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatRunRegistry.cs
    文件功能描述：让 AdminChat 生成在页面刷新或关闭后继续执行


    创建标识：Senparc - 20261107

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 按会话保存正在执行的 AdminChat 任务。页面断开只取消观察者，不取消生成。
/// 显式停止和进程退出才会取消。
/// </summary>
public sealed class AdminChatRunRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan FinishedVisibility = TimeSpan.FromSeconds(20);
    private readonly ConcurrentDictionary<int, AdminChatRun> _runs = new();
    private readonly ConcurrentDictionary<int, DateTimeOffset> _finishedAt = new();

    public AdminChatRunRegistry(IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            foreach (var run in _runs.Values)
                run.Cancellation.Cancel();
        });
    }

    public AdminChatRun TryBegin(int sessionId, int userId)
    {
        var run = new AdminChatRun(sessionId, userId);
        if (!_runs.TryAdd(sessionId, run))
        {
            run.Cancellation.Dispose();
            return null;
        }

        _finishedAt.TryRemove(sessionId, out _);
        return run;
    }

    public AdminChatRun Find(int sessionId, int userId)
    {
        if (sessionId <= 0 || userId <= 0) return null;
        return _runs.TryGetValue(sessionId, out var run) && run.UserId == userId ? run : null;
    }

    public bool Cancel(int sessionId, int userId)
    {
        var run = Find(sessionId, userId);
        if (run == null) return false;
        try
        {
            run.Cancellation.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public AdminChatRunStatus GetStatus(int sessionId, int userId)
    {
        var run = Find(sessionId, userId);
        if (run == null)
        {
            var justFinished = _finishedAt.TryGetValue(sessionId, out var finishedAt)
                && DateTimeOffset.UtcNow - finishedAt <= FinishedVisibility;
            return new AdminChatRunStatus(false, justFinished, "", "", "");
        }

        lock (run.Gate)
        {
            return new AdminChatRunStatus(true, false, run.AssistantText, run.ThinkingText, run.StatusText);
        }
    }

    public AdminChatRunListener Listen(AdminChatRun run)
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        string snapshot;
        lock (run.Gate)
        {
            run.Subscribers.Add(channel);
            snapshot = JsonSerializer.Serialize(new
            {
                text = run.AssistantText,
                thinking = run.ThinkingText,
                status = string.IsNullOrEmpty(run.StatusText) ? "任务仍在执行…" : run.StatusText
            }, JsonOptions);
        }

        return new AdminChatRunListener(channel, snapshot, () =>
        {
            lock (run.Gate) run.Subscribers.Remove(channel);
            channel.Writer.TryComplete();
        });
    }

    public void Publish(AdminChatRun run, string eventName, string json, Action<AdminChatRun> mutate = null)
    {
        Channel<string>[] targets;
        lock (run.Gate)
        {
            mutate?.Invoke(run);
            targets = run.Subscribers.ToArray();
        }

        var frame = eventName + "\n" + json;
        foreach (var target in targets)
            target.Writer.TryWrite(frame);
    }

    public void Finish(AdminChatRun run)
    {
        if (run == null) return;
        Channel<string>[] targets;
        lock (run.Gate)
        {
            if (run.Closed) return;
            run.Closed = true;
            targets = run.Subscribers.ToArray();
        }

        _runs.TryRemove(run.SessionId, out _);
        _finishedAt[run.SessionId] = DateTimeOffset.UtcNow;
        foreach (var target in targets)
            target.Writer.TryComplete();
        run.Cancellation.Dispose();
    }
}

public sealed class AdminChatRun
{
    public AdminChatRun(int sessionId, int userId)
    {
        SessionId = sessionId;
        UserId = userId;
        Cancellation = new CancellationTokenSource();
    }

    public int SessionId { get; }
    public int UserId { get; }
    public CancellationTokenSource Cancellation { get; }
    public object Gate { get; } = new();
    public List<Channel<string>> Subscribers { get; } = new();
    public string AssistantText { get; set; } = "";
    public string ThinkingText { get; set; } = "";
    public string StatusText { get; set; } = "";
    public bool Closed { get; set; }
    public CancellationToken Token => Cancellation.Token;
}

public sealed record AdminChatRunStatus(
    bool Running, bool JustFinished, string Text, string Thinking, string Status);

public sealed class AdminChatRunListener : IDisposable
{
    private readonly Action _dispose;
    public AdminChatRunListener(Channel<string> channel, string snapshotJson, Action dispose)
    {
        Channel = channel;
        SnapshotJson = snapshotJson;
        _dispose = dispose;
    }

    public Channel<string> Channel { get; }
    public string SnapshotJson { get; }
    public void Dispose() => _dispose();
}
