/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellModuleAvailabilityService.cs
    文件功能描述：解析允许向 Admin Footer 提供纽铃状态的已开放 XNCF 模块

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 新增后台同步管理与可配置多语言页脚

    修改标识：Senparc - 20260804
    修改描述：v0.3.0 将后台同步功能统一更名为 NeuBell/纽铃

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase;

namespace Senparc.Areas.Admin.Domain.Services;

public interface INeuBellModuleAvailabilityService
{
    Task<IReadOnlySet<string>> GetOpenModuleUidsAsync(
        IEnumerable<string> moduleUids,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 统一判断纽铃 Provider 所属模块是否可用。复用 NCF 现有注册表和
/// <see cref="XncfRegisterManager.CheckXncfAvailable(IXncfRegister)"/> 缓存链路，不在 Footer 请求中重复查询模块表。
/// </summary>
public sealed class NeuBellModuleAvailabilityService : INeuBellModuleAvailabilityService
{
    private readonly XncfRegisterManager _xncfRegisterManager;

    public NeuBellModuleAvailabilityService(IServiceProvider serviceProvider)
    {
        _xncfRegisterManager = new XncfRegisterManager(serviceProvider);
    }

    public async Task<IReadOnlySet<string>> GetOpenModuleUidsAsync(
        IEnumerable<string> moduleUids,
        CancellationToken cancellationToken = default)
    {
        var requestedUids = moduleUids?
            .Where(uid => !string.IsNullOrWhiteSpace(uid))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
        if (requestedUids.Length == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var requestedUidSet = requestedUids.ToHashSet(StringComparer.OrdinalIgnoreCase);
        // 先过滤运行时已加载的模块；仅存在 DLL/DI 注册不代表模块已安装并开放。
        var registeredModules = XncfRegisterManager.RegisterList
            .Where(register => requestedUidSet.Contains(register.Uid))
            .ToArray();
        var openModuleUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var register in registeredModules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // CheckXncfAvailable 还会校验持久化的开放状态，并复用 FullXncfModuleCache。
            if (await _xncfRegisterManager.CheckXncfAvailable(register).ConfigureAwait(false))
            {
                openModuleUids.Add(register.Uid);
            }
        }

        return openModuleUids;
    }
}
