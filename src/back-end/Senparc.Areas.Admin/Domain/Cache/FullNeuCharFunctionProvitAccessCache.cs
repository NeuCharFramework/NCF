/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FullNeuCharFunctionProvitAccessCache.cs
    文件功能描述：FullNeuCharFunctionProvitAccessCache（Function 全局 Provit 数据库访问策略全量缓存）：
    通过 CO2NET 缓存策略（IBaseObjectCacheStrategy / CacheStrategyFactory）将全部
    NeuCharFunctionProvitAccess 策略行缓存在内存，提供按（ModuleUid, FunctionKey）
    的 O(1) 查找，避免每次全局 Provit 访问都查询数据库。
    实现范式参考 Senparc.Ncf.Core 的 FullSystemConfigCache（BaseCache<T> + Update()）。
    策略行变更后需调用 RemoveCache() 使缓存失效。

    创建标识：Senparc - 20260917
    修改描述：v0.9.1 新增 Function 全局 Provit 数据库访问策略映射

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Log;

namespace Senparc.Areas.Admin.Domain.Cache;

/// <summary>
/// Function 全局 Provit 数据库访问策略的全量缓存数据。
/// <para>请作为只读快照使用，不要修改其中实体，以免污染共享缓存。</para>
/// </summary>
public class FullNeuCharFunctionProvitAccess
{
    /// <summary>
    /// 缓存生成时间（UTC）
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// 全部未删除的策略行
    /// </summary>
    public List<NeuCharFunctionProvitAccess> Items { get; set; } = new();

    /// <summary>
    /// policyKey（moduleUid|functionKey，不区分大小写）到策略行的索引
    /// </summary>
    public Dictionary<string, NeuCharFunctionProvitAccess> Index { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 按（ModuleUid, FunctionKey）查找策略；不存在返回 null
    /// </summary>
    public NeuCharFunctionProvitAccess Find(string moduleUid, string functionKey)
    {
        if (Index == null)
        {
            return null;
        }
        return Index.TryGetValue(
            NeuCharFunctionProvitAccess.GetPolicyKey(moduleUid, functionKey),
            out var item) ? item : null;
    }
}

/// <summary>
/// Function 全局 Provit 数据库访问策略全量缓存。
/// </summary>
public class FullNeuCharFunctionProvitAccessCache : BaseCache<FullNeuCharFunctionProvitAccess>
{
    public const string CACHE_KEY = "FullNeuCharFunctionProvitAccessCache";

    private INcfDbData _dataContext => base._db as INcfDbData;

    public FullNeuCharFunctionProvitAccessCache(INcfDbData db)
        : base(CACHE_KEY, db)
    {
        // 策略变更需手动 RemoveCache，默认 30 分钟兜底过期
        base.TimeOut = 30;
    }

    public override FullNeuCharFunctionProvitAccess Update()
    {
        List<NeuCharFunctionProvitAccess> items = new();
        try
        {
            items = _dataContext.BaseDataContext
                .Set<NeuCharFunctionProvitAccess>()
                .Where(z => !z.Flag)
                .OrderBy(z => z.ModuleUid)
                .ThenBy(z => z.FunctionKey)
                .ToList();
        }
        catch (Exception ex)
        {
            // 首次安装、架构升级中或数据库暂不可用时，降级为空策略集，
            // 让全局 Provit 回退到代码属性基线，避免影响其他功能。
            var msg = $"FullNeuCharFunctionProvitAccessCache 访问数据库异常，降级为空策略集（回退代码属性基线）。\n{ex.Message}";
            LogUtility.SystemLogger.Debug(msg, ex);
        }

        var full = new FullNeuCharFunctionProvitAccess
        {
            UpdatedAtUtc = DateTime.UtcNow,
            Items = items,
            Index = BuildIndex(items)
        };

        base.SetData(full, base.TimeOut, null);
        return base.Data;
    }

    /// <summary>
    /// 构建 policyKey 索引。个别数据库的默认排序规则区分大小写（如 PostgreSQL），
    /// 理论上可能存在仅大小写不同的键；索引按“后写入优先”去重，避免 ToDictionary 抛异常。
    /// </summary>
    private static Dictionary<string, NeuCharFunctionProvitAccess> BuildIndex(
        List<NeuCharFunctionProvitAccess> items)
    {
        var index = new Dictionary<string, NeuCharFunctionProvitAccess>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            index[NeuCharFunctionProvitAccess.GetPolicyKey(item.ModuleUid, item.FunctionKey)] = item;
        }
        return index;
    }
}
