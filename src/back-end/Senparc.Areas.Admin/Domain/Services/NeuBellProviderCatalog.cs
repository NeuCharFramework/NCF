/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellProviderCatalog.cs
    文件功能描述：按 XNCF 安装和开放状态筛选 Admin Footer 纽铃 Provider

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 新增后台同步管理与可配置多语言页脚

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 将后台同步功能统一更名为 NeuBell/纽铃

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 纽铃 Provider 的唯一可用性入口：去重、稳定排序，并只返回所属 XNCF 已开放的 Provider。
/// </summary>
public sealed class NeuBellProviderCatalog
{
    private readonly IReadOnlyList<INeuBellProvider> _providers;
    private readonly INeuBellModuleAvailabilityService _moduleAvailabilityService;
    private readonly ILogger<NeuBellProviderCatalog> _logger;

    public NeuBellProviderCatalog(
        IEnumerable<INeuBellProvider> providers,
        INeuBellModuleAvailabilityService moduleAvailabilityService,
        ILogger<NeuBellProviderCatalog> logger)
    {
        _providers = providers
            .GroupBy(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _moduleAvailabilityService = moduleAvailabilityService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<INeuBellProvider>> GetAvailableProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var declaredProviders = _providers
            .Where(provider => !string.IsNullOrWhiteSpace(provider.ModuleUid))
            .ToArray();
        // 旧二进制 Provider 因默认接口实现仍可被加载，但无法证明模块状态时不得执行。
        foreach (var provider in _providers.Except(declaredProviders))
        {
            _logger.LogDebug(
                "纽铃 Provider {ProviderId} 未声明 ModuleUid，已忽略以避免未安装模块参与 Footer。",
                provider.ProviderId);
        }

        if (declaredProviders.Length == 0)
        {
            return Array.Empty<INeuBellProvider>();
        }

        try
        {
            var openModuleUids = await _moduleAvailabilityService
                .GetOpenModuleUidsAsync(
                    declaredProviders.Select(provider => provider.ModuleUid),
                    cancellationToken)
                .ConfigureAwait(false);
            return declaredProviders
                .Where(provider => openModuleUids.Contains(provider.ModuleUid))
                .ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 模块状态不可判定时 fail closed，避免可选模块未安装仍在 Footer 运行。
            _logger.LogWarning(ex, "读取 XNCF 模块开放状态失败，纽铃 Provider 将按安全方式全部禁用。");
            return Array.Empty<INeuBellProvider>();
        }
    }
}
