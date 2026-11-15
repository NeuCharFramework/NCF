/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotBoardModels.cs
    文件功能描述：NeuCharPivotBoard（Pivot 面板 / Provit Panel）实体：
    为特定页面（如后台首页）聚合各 XNCF 模块 Pivot 的 Function 块

    创建标识：Senparc - 20260901

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// Pivot 面板（Provit Panel）：绑定到某个页面（PageKey），
/// 内部由若干 Provit Block 组成，每个 Block 引用任意 XNCF 模块 Pivot 的 Function。
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuCharPivotBoard))]
[Serializable]
public class NeuCharPivotBoard : EntityBase<int>
{
    /// <summary>
    /// 面板绑定的页面标识，例如 admin-home
    /// </summary>
    [Required, MaxLength(100)]
    public string PageKey { get; private set; }

    [Required, MaxLength(200)]
    public string Name { get; private set; }

    public string Description { get; private set; }

    /// <summary>
    /// 面板内块（Block）的列数
    /// </summary>
    public int Columns { get; private set; }

    /// <summary>
    /// 块列表 JSON 数组：[{ key, moduleUid, functionKey, functionName, title, summary, accent, exposedParameters[] }]
    /// </summary>
    public string BlocksJson { get; private set; }

    /// <summary>
    /// 是否启用（启用后才会渲染到对应页面）
    /// </summary>
    public bool IsEnabled { get; private set; }

    public int AdminUserId { get; private set; }

    private NeuCharPivotBoard() { }

    public NeuCharPivotBoard(string pageKey, string name, int adminUserId)
    {
        PageKey = NormalizePageKey(pageKey);
        Name = name?.Trim() ?? string.Empty;
        AdminUserId = adminUserId;
        Description = string.Empty;
        Columns = 2;
        BlocksJson = "[]";
        IsEnabled = true;
    }

    public void UpdateInfo(string name, string description, int columns, bool isEnabled)
    {
        Name = string.IsNullOrWhiteSpace(name) ? Name : name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Columns = Math.Clamp(columns <= 0 ? 2 : columns, 1, 6);
        IsEnabled = isEnabled;
        SetUpdateTime();
    }

    public void RebindPage(string pageKey)
    {
        PageKey = NormalizePageKey(pageKey);
        SetUpdateTime();
    }

    public void SetBlocks(string blocksJson)
    {
        BlocksJson = string.IsNullOrWhiteSpace(blocksJson) ? "[]" : blocksJson;
        SetUpdateTime();
    }

    public static string NormalizePageKey(string pageKey)
    {
        pageKey = (pageKey ?? string.Empty).Trim().ToLowerInvariant();
        if (pageKey.StartsWith('/'))
        {
            pageKey = pageKey.TrimStart('/');
        }
        return string.IsNullOrEmpty(pageKey) ? "default" : pageKey;
    }
}
