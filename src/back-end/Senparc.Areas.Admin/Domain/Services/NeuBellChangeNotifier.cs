/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellChangeNotifier.cs
    文件功能描述：纽铃模块变更通知与订阅

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 新增后台同步管理与可配置多语言页脚

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 将后台同步功能统一更名为 NeuBell/纽铃

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class NeuBellChangeNotifier : INeuBellPublisher
{
    private const int SubscriberCapacity = 64;
    private readonly object _syncRoot = new();
    private readonly Dictionary<Guid, Channel<string>> _subscribers = new();
    private readonly ConcurrentDictionary<string, long> _providerRevisions =
        new(StringComparer.OrdinalIgnoreCase);

    public ValueTask NotifyChangedAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return ValueTask.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _providerRevisions.AddOrUpdate(providerId, 1, (_, revision) => revision + 1);

        Channel<string>[] subscribers;
        lock (_syncRoot)
        {
            subscribers = _subscribers.Values.ToArray();
        }

        foreach (var subscriber in subscribers)
        {
            subscriber.Writer.TryWrite(providerId);
        }

        return ValueTask.CompletedTask;
    }

    public long GetRevision(string providerId)
    {
        return _providerRevisions.TryGetValue(providerId, out var revision) ? revision : 0;
    }

    public async IAsyncEnumerable<string> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(SubscriberCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_syncRoot)
        {
            _subscribers[subscriptionId] = channel;
        }

        try
        {
            await foreach (var providerId in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return providerId;
            }
        }
        finally
        {
            lock (_syncRoot)
            {
                _subscribers.Remove(subscriptionId);
            }

            channel.Writer.TryComplete();
        }
    }
}
