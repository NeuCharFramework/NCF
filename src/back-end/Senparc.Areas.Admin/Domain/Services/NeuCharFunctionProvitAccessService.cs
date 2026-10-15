/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharFunctionProvitAccessService.cs
    文件功能描述：NeuCharFunctionProvitAccess（Function 全局 Provit 数据库访问策略）服务：
    策略读取（走 FullNeuCharFunctionProvitAccessCache 缓存）、单条/批量设置与清除。
    策略行冗余保留：不随 XNCF 模块清除而删除，仅本服务的清除操作（手动）才会删除。

    创建标识：Senparc - 20260917
    修改描述：v0.9.1 新增 Function 全局 Provit 数据库访问策略映射

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Cache;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 单条策略输入（页面保存 / 批量设置共用）
/// </summary>
public sealed class NeuCharFunctionProvitAccessInput
{
    public string ModuleUid { get; set; }
    public string FunctionKey { get; set; }
    public int AccessMode { get; set; } = NeuCharFunctionProvitAccess.AccessModeInherit;
    public List<string> AllowedRoleCodes { get; set; } = new();
    public List<string> AllowedPermissionCodes { get; set; } = new();
    public List<int> AllowedUserIds { get; set; } = new();
    public string Remark { get; set; }
}

/// <summary>
/// Function 全局 Provit 数据库访问策略服务
/// </summary>
public sealed class NeuCharFunctionProvitAccessService : BaseClientService<NeuCharFunctionProvitAccess>
{
    private readonly INeuCharFunctionProvitAccessRepository _repository;
    private readonly FullNeuCharFunctionProvitAccessCache _cache;

    public NeuCharFunctionProvitAccessService(
        INeuCharFunctionProvitAccessRepository repository,
        IServiceProvider serviceProvider,
        FullNeuCharFunctionProvitAccessCache cache)
        : base(repository, serviceProvider)
    {
        _repository = repository;
        _cache = cache;
    }

    #region 读取（缓存优先）

    /// <summary>
    /// 获取指定（ModuleUid, FunctionKey）的策略；不存在返回 null。走缓存，O(1) 查找。
    /// </summary>
    public NeuCharFunctionProvitAccess GetPolicy(string moduleUid, string functionKey)
    {
        var full = _cache.Data;
        return full?.Find(moduleUid, functionKey);
    }

    /// <summary>
    /// 获取全部策略（只读快照）。走缓存。
    /// </summary>
    public IReadOnlyList<NeuCharFunctionProvitAccess> GetAllPolicies()
    {
        var full = _cache.Data;
        return full?.Items is { Count: > 0 } items
            ? items
            : Array.Empty<NeuCharFunctionProvitAccess>();
    }

    #endregion

    #region 写入（写后失效缓存）

    /// <summary>
    /// 保存（新增或更新）单条策略
    /// </summary>
    public async Task<NeuCharFunctionProvitAccess> UpsertPolicyAsync(
        NeuCharFunctionProvitAccessInput input,
        CancellationToken cancellationToken = default)
    {
        if (input == null ||
            string.IsNullOrWhiteSpace(input.ModuleUid) ||
            string.IsNullOrWhiteSpace(input.FunctionKey))
        {
            throw new ArgumentException("ModuleUid 与 FunctionKey 不能为空。");
        }

        var moduleUid = NeuCharFunctionProvitAccess.NormalizeModuleUid(input.ModuleUid);
        var functionKey = NeuCharFunctionProvitAccess.NormalizeFunctionKey(input.FunctionKey);

        var policy = await base.GetObjectAsync(
                z => z.ModuleUid == moduleUid && z.FunctionKey == functionKey && !z.Flag)
            .ConfigureAwait(false)
            ?? new NeuCharFunctionProvitAccess(moduleUid, functionKey);

        policy.Update(
            input.AccessMode,
            input.AllowedRoleCodes,
            input.AllowedPermissionCodes,
            input.AllowedUserIds,
            input.Remark);
        await base.SaveObjectAsync(policy).ConfigureAwait(false);
        await _cache.RemoveCacheAsync().ConfigureAwait(false);
        return policy;
    }

    /// <summary>
    /// 清除（删除）单条策略。
    /// <para>策略为配置行：手动清除时物理删除，避免软删除残留与唯一索引冲突；
    /// 模块清除场景不会调用本方法，策略行冗余保留待模块重装后继续生效。</para>
    /// </summary>
    public async Task<bool> DeletePolicyAsync(
        string moduleUid,
        string functionKey,
        CancellationToken cancellationToken = default)
    {
        var policy = await base.GetObjectAsync(
                z => z.ModuleUid == NeuCharFunctionProvitAccess.NormalizeModuleUid(moduleUid)
                    && z.FunctionKey == NeuCharFunctionProvitAccess.NormalizeFunctionKey(functionKey)
                    && !z.Flag)
            .ConfigureAwait(false);
        if (policy == null)
        {
            return false;
        }
        await _repository.DeleteAsync(policy, softDelete: false).ConfigureAwait(false);
        await _cache.RemoveCacheAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 批量保存策略（逐条校验，全部完成后统一失效一次缓存）
    /// </summary>
    /// <returns>成功保存的数量</returns>
    public async Task<int> BatchUpsertAsync(
        IEnumerable<NeuCharFunctionProvitAccessInput> inputs,
        CancellationToken cancellationToken = default)
    {
        var saved = 0;
        foreach (var input in inputs ?? Array.Empty<NeuCharFunctionProvitAccessInput>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (input == null ||
                string.IsNullOrWhiteSpace(input.ModuleUid) ||
                string.IsNullOrWhiteSpace(input.FunctionKey))
            {
                continue;
            }
            var moduleUid = NeuCharFunctionProvitAccess.NormalizeModuleUid(input.ModuleUid);
            var functionKey = NeuCharFunctionProvitAccess.NormalizeFunctionKey(input.FunctionKey);

            var policy = await base.GetObjectAsync(
                    z => z.ModuleUid == moduleUid && z.FunctionKey == functionKey && !z.Flag)
                .ConfigureAwait(false)
                ?? new NeuCharFunctionProvitAccess(moduleUid, functionKey);

            policy.Update(
                input.AccessMode,
                input.AllowedRoleCodes,
                input.AllowedPermissionCodes,
                input.AllowedUserIds,
                input.Remark);
            await base.SaveObjectAsync(policy).ConfigureAwait(false);
            saved++;
        }
        if (saved > 0)
        {
            await _cache.RemoveCacheAsync().ConfigureAwait(false);
        }
        return saved;
    }

    /// <summary>
    /// 批量清除策略
    /// </summary>
    /// <returns>成功清除的数量</returns>
    public async Task<int> BatchDeleteAsync(
        IEnumerable<(string ModuleUid, string FunctionKey)> keys,
        CancellationToken cancellationToken = default)
    {
        var deleted = 0;
        foreach (var (moduleUid, functionKey) in keys ?? Array.Empty<(string, string)>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var policy = await base.GetObjectAsync(
                    z => z.ModuleUid == NeuCharFunctionProvitAccess.NormalizeModuleUid(moduleUid)
                        && z.FunctionKey == NeuCharFunctionProvitAccess.NormalizeFunctionKey(functionKey)
                        && !z.Flag)
                .ConfigureAwait(false);
            if (policy == null)
            {
                continue;
            }
            await _repository.DeleteAsync(policy, softDelete: false).ConfigureAwait(false);
            deleted++;
        }
        if (deleted > 0)
        {
            await _cache.RemoveCacheAsync().ConfigureAwait(false);
        }
        return deleted;
    }

    #endregion
}
