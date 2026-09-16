/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellTestProvider.cs
    文件功能描述：为 Admin Function 提供可发送、可消费的纽铃提醒示例


    创建标识：Senparc - 20260805

    修改标识：Senparc - 20260808
    修改描述：v0.4.0 新增纽铃可见提醒示例提供程序

    修改标识：Senparc - 20260813
    修改描述：v0.5.0 集成 NeuCharPivot 与 NeuCharWorkflow 管理能力并优化后台体验

    修改标识：Senparc - 20260914
    修改描述：v0.7.1 新增 SendAndGetItem（发送并返回 NeuBellItem），
    供 WebHook 创建通知（item-created）携带完整条目数据

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 仅保存在当前 Admin Host 进程内的纽铃测试状态，不写入数据库，也不修改业务 Provider 数据。
/// </summary>
public sealed class NeuBellTestProvider : INeuBellProvider, INeuBellConsumableProvider
{
    public const string ProviderIdValue = "admin-neubell-test";
    public const string ItemIdPrefix = "function-reminder-";
    public const string ItemTitle = "NeuBell 测试提醒";
    public const string ItemSummary = "由 Function 发送；可在 Function 中消费本条，或清除当前订阅全部提醒。";
    public const string ItemSeverity = "warning";
    public const string ItemDetailUrl = "/Admin/Index";

    private readonly object _syncRoot = new();
    private readonly List<Reminder> _pendingReminders = new();
    private int _sequence;

    private sealed record Reminder(string Id, DateTimeOffset CreatedAt);

    private static NeuBellItem ToNeuBellItem(Reminder reminder)
    {
        return new NeuBellItem(
            reminder.Id,
            ItemTitle,
            ItemSummary,
            1,
            ItemSeverity,
            ItemDetailUrl,
            reminder.CreatedAt);
    }

    public string ProviderId => ProviderIdValue;

    public string ModuleUid => Register.ModuleUid;

    /// <summary>
    /// 当前待消费提醒数量
    /// </summary>
    public int PendingCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _pendingReminders.Count;
            }
        }
    }

    public int Send()
    {
        lock (_syncRoot)
        {
            if (_pendingReminders.Count < int.MaxValue)
            {
                _pendingReminders.Add(new Reminder(
                    ItemIdPrefix + ++_sequence,
                    DateTimeOffset.Now));
            }
            return _pendingReminders.Count;
        }
    }

    /// <summary>
    /// 发送一条提醒并返回映射后的 <see cref="NeuBellItem"/>
    /// （供 WebHook 创建通知携带完整条目数据）
    /// </summary>
    public NeuBellItem SendAndGetItem()
    {
        Reminder reminder;
        lock (_syncRoot)
        {
            reminder = new Reminder(
                ItemIdPrefix + ++_sequence,
                DateTimeOffset.Now);
            if (_pendingReminders.Count < int.MaxValue)
            {
                _pendingReminders.Add(reminder);
            }
        }
        return ToNeuBellItem(reminder);
    }

    public int ConsumeLatest()
    {
        return ConsumeLatestAndGetItem() == null ? 0 : 1;
    }

    public NeuBellItem ConsumeLatestAndGetItem()
    {
        Reminder reminder;
        lock (_syncRoot)
        {
            if (_pendingReminders.Count == 0)
            {
                return null;
            }
            var index = _pendingReminders.Count - 1;
            reminder = _pendingReminders[index];
            _pendingReminders.RemoveAt(index);
        }
        return ToNeuBellItem(reminder);
    }

    public int ConsumeAll()
    {
        return ConsumeAllAndGetItems().Count;
    }

    public IReadOnlyList<NeuBellItem> ConsumeAllAndGetItems()
    {
        Reminder[] reminders;
        lock (_syncRoot)
        {
            reminders = _pendingReminders.ToArray();
            _pendingReminders.Clear();
        }
        return reminders
            .OrderByDescending(item => item.CreatedAt)
            .Select(ToNeuBellItem)
            .ToArray();
    }

    public ValueTask<int> ConsumeItemAsync(
        NeuBellRequestContext context,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            var index = _pendingReminders.FindIndex(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
            if (index < 0)
            {
                return ValueTask.FromResult(0);
            }
            _pendingReminders.RemoveAt(index);
            return ValueTask.FromResult(1);
        }
    }

    ValueTask<int> INeuBellConsumableProvider.ConsumeAllAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken) => ValueTask.FromResult(ConsumeAll());

    public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Reminder> pendingReminders;
        lock (_syncRoot)
        {
            pendingReminders = _pendingReminders.ToArray();
        }

        IReadOnlyList<NeuBellItem> items = pendingReminders
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new NeuBellItem(
                item.Id,
                ItemTitle,
                ItemSummary,
                1,
                ItemSeverity,
                ItemDetailUrl,
                item.CreatedAt))
            .ToArray();

        return ValueTask.FromResult(new NeuBellSnapshot(
            ProviderId,
            ModuleUid,
            "NeuBell 测试",
            "fa fa-bell-o",
            true,
            items));
    }
}
