/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SynchroSnapshotService.cs
    文件功能描述：聚合 XNCF Synchro Provider 并使用 NCF 全局缓存策略缓存快照

    创建标识：Senparc - 20260802

    修改标识：Senparc - 20260804
    修改描述：v0.2.1 聚合并缓存带超时保护的 Provider 快照
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Shared.Abstractions.Synchro;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class SynchroSnapshotService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(5);
    private readonly SynchroProviderCatalog _providerCatalog;
    private readonly SynchroChangeNotifier _changeNotifier;
    private readonly ILogger<SynchroSnapshotService> _logger;

    public SynchroSnapshotService(
        SynchroProviderCatalog providerCatalog,
        SynchroChangeNotifier changeNotifier,
        ILogger<SynchroSnapshotService> logger)
    {
        _providerCatalog = providerCatalog;
        _changeNotifier = changeNotifier;
        _logger = logger;
    }

    public async Task<bool> HasAvailableProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var providers = await _providerCatalog.GetAvailableProvidersAsync(cancellationToken)
            .ConfigureAwait(false);
        return providers.Count > 0;
    }

    public async Task<IReadOnlyList<SynchroSnapshot>> GetSnapshotsAsync(
        SynchroRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var providers = await _providerCatalog.GetAvailableProvidersAsync(cancellationToken)
            .ConfigureAwait(false);
        if (providers.Count == 0)
        {
            return Array.Empty<SynchroSnapshot>();
        }

        var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
        // Provider 之间没有依赖，并行读取可避免总延迟随模块数线性增长。
        var snapshotTasks = providers
            .Select(provider => GetSnapshotAsync(provider, context, cache, cancellationToken))
            .ToArray();
        var snapshots = await Task.WhenAll(snapshotTasks).ConfigureAwait(false);
        return snapshots.Where(snapshot => snapshot != null).ToArray();
    }

    private async Task<SynchroSnapshot> GetSnapshotAsync(
        ISynchroProvider provider,
        SynchroRequestContext context,
        IBaseObjectCacheStrategy cache,
        CancellationToken cancellationToken)
    {
        var revision = _changeNotifier.GetRevision(provider.ProviderId);
        var cacheKey = $"NCF:Synchro:{context.TenantId ?? "default"}:{context.UserId}:{provider.ProviderId}:{revision}";

        try
        {
            var snapshot = cache.Get<SynchroSnapshot>(cacheKey);
            if (snapshot == null)
            {
                // 单个可选模块不得长时间占用 Footer 状态请求。
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ProviderTimeout);
                snapshot = await provider.GetSnapshotAsync(context, timeout.Token).ConfigureAwait(false);
                if (snapshot != null)
                {
                    cache.Set(cacheKey, snapshot, CacheDuration);
                }
            }

            if (snapshot != null
                && !string.Equals(snapshot.ModuleUid, provider.ModuleUid, StringComparison.OrdinalIgnoreCase))
            {
                // 拒绝身份不一致的快照，防止 Provider 绕过模块开放状态筛选。
                _logger.LogWarning(
                    "Synchro Provider {ProviderId} 返回了不匹配的 ModuleUid {SnapshotModuleUid}，已忽略。",
                    provider.ProviderId,
                    snapshot.ModuleUid);
                return null;
            }

            return snapshot;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Synchro Provider {ProviderId} 快照获取失败。", provider.ProviderId);
            return null;
        }
    }
}
