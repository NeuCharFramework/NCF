/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawNeuBellPushHostedService.cs
    文件功能描述：WeixinClawNeuBellPushHostedService.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.WeixinClawIntegration;

/// <summary>
/// 将 NeuBell 的变更事件转发给已绑定的个人微信会话。
/// 变更通知只触发重新读取快照，不把业务正文放进底层发布事件。
/// </summary>
public sealed class WeixinClawNeuBellPushHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NeuBellChangeNotifier _changeNotifier;
    private readonly IOptions<WeixinClawAdminIntegrationOptions> _options;
    private readonly ILogger<WeixinClawNeuBellPushHostedService> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastPush = new(StringComparer.OrdinalIgnoreCase);

    public WeixinClawNeuBellPushHostedService(
        IServiceScopeFactory scopeFactory,
        NeuBellChangeNotifier changeNotifier,
        IOptions<WeixinClawAdminIntegrationOptions> options,
        ILogger<WeixinClawNeuBellPushHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _changeNotifier = changeNotifier;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled || !_options.Value.EnableNeuBell)
        {
            return;
        }

        try
        {
            await PushAllProvidersAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NeuBell 启动补偿转发失败。");
        }

        await foreach (var providerId in _changeNotifier.SubscribeAsync(stoppingToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            if (_lastPush.TryGetValue(providerId, out var previous)
                && now - previous < TimeSpan.FromSeconds(2))
            {
                continue;
            }
            _lastPush[providerId] = now;

            try
            {
                await PushProviderAsync(providerId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NeuBell 变更转发到个人微信失败：ProviderId={ProviderId}", providerId);
            }
        }
    }

    private async Task PushProviderAsync(string providerId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var bindingService = scope.ServiceProvider.GetRequiredService<WeixinClawAdminBindingService>();
        var snapshotService = scope.ServiceProvider.GetRequiredService<NeuBellSnapshotService>();
        var sender = scope.ServiceProvider.GetRequiredService<Senparc.Xncf.WeixinManager.WeixinClaw.IWeixinClawMessageSender>();
        var bindings = await bindingService.GetNeuBellBindingsAsync().ConfigureAwait(false);

        foreach (var binding in bindings)
        {
            var snapshots = await snapshotService.GetSnapshotsAsync(
                new NeuBellRequestContext(binding.AdminUserId.ToString()),
                cancellationToken).ConfigureAwait(false);
            var snapshot = snapshots.FirstOrDefault(item =>
                string.Equals(item.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
            await PushSnapshotAsync(binding, snapshot, sender, bindingService, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PushAllProvidersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var bindingService = scope.ServiceProvider.GetRequiredService<WeixinClawAdminBindingService>();
        var snapshotService = scope.ServiceProvider.GetRequiredService<NeuBellSnapshotService>();
        var sender = scope.ServiceProvider.GetRequiredService<Senparc.Xncf.WeixinManager.WeixinClaw.IWeixinClawMessageSender>();
        var bindings = await bindingService.GetNeuBellBindingsAsync().ConfigureAwait(false);

        foreach (var binding in bindings)
        {
            var snapshots = await snapshotService.GetSnapshotsAsync(
                new NeuBellRequestContext(binding.AdminUserId.ToString()),
                cancellationToken).ConfigureAwait(false);
            foreach (var snapshot in snapshots)
            {
                await PushSnapshotAsync(binding, snapshot, sender, bindingService, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task PushSnapshotAsync(
        WeixinClawAdminBinding binding,
        NeuBellSnapshot snapshot,
        Senparc.Xncf.WeixinManager.WeixinClaw.IWeixinClawMessageSender sender,
        WeixinClawAdminBindingService bindingService,
        CancellationToken cancellationToken)
    {
        var items = snapshot?.Items
            ?.Where(item => item.Count > 0 || !string.Equals(item.Severity, "info", StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();
        if (snapshot == null || items == null || items.Count == 0)
        {
            return;
        }

        var fingerprint = ComputeFingerprint(snapshot.ProviderId, items);
        if (binding.HasNeuBellPush(snapshot.ProviderId, fingerprint))
        {
            return;
        }

        var lines = items.Select(item => $"{item.Title}：{item.Summary}{(item.Count > 0 ? $"（{item.Count}）" : string.Empty)}");
        var message = $"NeuBell「{snapshot.DisplayName}」有新的提醒：{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
        try
        {
            await sender.SendTextAsync(
                binding.AccountId,
                binding.FromUserId,
                message,
                binding.ContextToken,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("上下文", StringComparison.Ordinal))
        {
            _logger.LogWarning(
                ex,
                "NeuBell 推送跳过：微信账号 {AccountId} 没有可用上下文，等待下一条手机入站消息。",
                binding.AccountId);
            return;
        }

        binding.MarkNeuBellPushed(snapshot.ProviderId, fingerprint);
        await bindingService.SaveObjectAsync(binding).ConfigureAwait(false);
    }

    private static string ComputeFingerprint(string providerId, IReadOnlyList<NeuBellItem> items)
    {
        var input = string.Join(
            "\n",
            new[] { providerId ?? string.Empty }
                .Concat(items.Select(item => string.Join(
                    "|",
                    item.Id,
                    item.Title,
                    item.Summary,
                    item.Count,
                    item.Severity,
                    item.UpdatedAt.ToUnixTimeMilliseconds()))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
