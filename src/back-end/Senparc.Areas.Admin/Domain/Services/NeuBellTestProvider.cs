/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellTestProvider.cs
    文件功能描述：为 Admin Function 提供可发送、可消费的纽铃提醒示例

    创建标识：Senparc - 20260805

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 仅保存在当前 Admin Host 进程内的纽铃测试状态，不写入数据库，也不修改业务 Provider 数据。
/// </summary>
public sealed class NeuBellTestProvider : INeuBellProvider
{
    public const string ProviderIdValue = "admin-neubell-test";
    public const string ItemIdValue = "function-reminder";

    private readonly object _syncRoot = new();
    private int _pendingCount;
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;

    public string ProviderId => ProviderIdValue;

    public string ModuleUid => Register.ModuleUid;

    public int Send()
    {
        lock (_syncRoot)
        {
            _pendingCount = _pendingCount == int.MaxValue ? int.MaxValue : _pendingCount + 1;
            _updatedAt = DateTimeOffset.Now;
            return _pendingCount;
        }
    }

    public int ConsumeAll()
    {
        lock (_syncRoot)
        {
            var consumedCount = _pendingCount;
            _pendingCount = 0;
            _updatedAt = DateTimeOffset.Now;
            return consumedCount;
        }
    }

    public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int pendingCount;
        DateTimeOffset updatedAt;
        lock (_syncRoot)
        {
            pendingCount = _pendingCount;
            updatedAt = _updatedAt;
        }

        IReadOnlyList<NeuBellItem> items = pendingCount > 0
            ?
            [
                new NeuBellItem(
                    ItemIdValue,
                    "NeuBell 测试提醒",
                    $"已收到 {pendingCount} 条 Function 测试提醒，可在 Function 中选择“消费提醒”清除。",
                    pendingCount,
                    "warning",
                    "/Admin/Index",
                    updatedAt)
            ]
            : Array.Empty<NeuBellItem>();

        return ValueTask.FromResult(new NeuBellSnapshot(
            ProviderId,
            ModuleUid,
            "NeuBell 测试",
            "fa fa-bell-o",
            true,
            items));
    }
}
