/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharFunctionProvitAccessModels.cs
    文件功能描述：NeuCharFunctionProvitAccess（Function 全局 Provit 数据库访问策略）实体：
    按（ModuleUid, FunctionKey）为 Function 的全局 Pivot（Provit 浮动调用）建立
    数据库访问策略映射，可绑定用户、角色或权限码。数据库策略存在时覆盖
    FunctionRenderAttribute 中 AllowGlobalPivot / GlobalPivotRoleCodes /
    GlobalPivotPermissionCodes 的代码级约束；模块被清除后策略行冗余保留，
    模块再次安装时自动继续生效，仅手动清除才会删除。

    设计参考：Ontology 风格的“主体–资源–效果”访问控制三元组思想：
    资源（Resource）= ModuleUid + FunctionKey 稳定标识；
    主体（Subject）= 用户（User）/ 角色（Role）/ 权限码（Permission）多维度绑定；
    效果（Effect）= 显式策略（Inherit/Open/Restricted/Deny），默认继承代码基线，
    显式策略优先（explicit-over-implicit）。

    创建标识：Senparc - 20260917
    修改描述：v0.9.1 新增 Function 全局 Provit 数据库访问策略映射

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// Function 全局 Provit（全局 Pivot 浮动调用）数据库访问策略。
/// <para>
/// 每个（ModuleUid, FunctionKey）最多一条策略。策略存在且 AccessMode 不为
/// <see cref="AccessModeInherit"/> 时，覆盖代码属性定义的访问约束。
/// 策略行不随 XNCF 模块清除而删除（冗余保留），模块重装后自动继续生效。
/// </para>
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuCharFunctionProvitAccess))]
[Serializable]
public class NeuCharFunctionProvitAccess : EntityBase<int>
{
    #region AccessMode 常量

    /// <summary>
    /// 继承代码属性（AllowGlobalPivot / GlobalPivotRoleCodes / GlobalPivotPermissionCodes）
    /// </summary>
    public const int AccessModeInherit = 0;
    /// <summary>
    /// 开放：任意已登录后台管理员可访问（忽略代码属性约束）
    /// </summary>
    public const int AccessModeOpen = 1;
    /// <summary>
    /// 受限：仅绑定的用户 / 角色 / 权限码可访问（三者任一命中即放行）
    /// </summary>
    public const int AccessModeRestricted = 2;
    /// <summary>
    /// 禁用：显式拒绝所有访问（覆盖代码属性的允许声明）
    /// </summary>
    public const int AccessModeDeny = 3;

    #endregion

    /// <summary>
    /// XNCF 模块 UID（稳定资源标识的一部分）
    /// </summary>
    [Required, MaxLength(100)]
    public string ModuleUid { get; private set; }

    /// <summary>
    /// Function Key（稳定资源标识的另一部分）
    /// </summary>
    [Required, MaxLength(200)]
    public string FunctionKey { get; private set; }

    /// <summary>
    /// 访问策略模式：0=继承代码 1=开放 2=受限 3=禁用
    /// </summary>
    [Required]
    public int AccessMode { get; private set; }

    /// <summary>
    /// 受限模式允许的角色码，逗号分隔
    /// </summary>
    [MaxLength(2000)]
    public string AllowedRoleCodes { get; private set; }

    /// <summary>
    /// 受限模式允许的权限资源码，逗号分隔
    /// </summary>
    [MaxLength(2000)]
    public string AllowedPermissionCodes { get; private set; }

    /// <summary>
    /// 受限模式允许的后台管理员用户 ID，逗号分隔
    /// </summary>
    [MaxLength(2000)]
    public string AllowedUserIds { get; private set; }

    private NeuCharFunctionProvitAccess() { }

    public NeuCharFunctionProvitAccess(string moduleUid, string functionKey)
    {
        ModuleUid = NormalizeModuleUid(moduleUid);
        FunctionKey = NormalizeFunctionKey(functionKey);
        AccessMode = AccessModeInherit;
        AllowedRoleCodes = string.Empty;
        AllowedPermissionCodes = string.Empty;
        AllowedUserIds = string.Empty;
    }

    /// <summary>
    /// 更新策略（Restricted 模式要求至少绑定一个主体）
    /// </summary>
    /// <exception cref="ArgumentException">Restricted 模式但没有绑定任何主体时抛出</exception>
    public void Update(
        int accessMode,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<string> permissionCodes,
        IReadOnlyList<int> adminUserIds,
        string remark = null)
    {
        if (accessMode is not (AccessModeInherit or AccessModeOpen or AccessModeRestricted or AccessModeDeny))
        {
            throw new ArgumentException($"不支持的 AccessMode：{accessMode}");
        }
        var roles = JoinCodes(roleCodes);
        var permissions = JoinCodes(permissionCodes);
        var users = JoinUserIds(adminUserIds);
        if (accessMode == AccessModeRestricted &&
            string.IsNullOrEmpty(roles) &&
            string.IsNullOrEmpty(permissions) &&
            string.IsNullOrEmpty(users))
        {
            throw new ArgumentException("受限（Restricted）策略必须至少绑定一个用户、角色或权限码。");
        }
        AccessMode = accessMode;
        AllowedRoleCodes = roles;
        AllowedPermissionCodes = permissions;
        AllowedUserIds = users;
        Remark = string.IsNullOrWhiteSpace(remark) ? Remark : remark.Trim();
        SetUpdateTime();
    }

    /// <summary>
    /// 绑定角色码列表
    /// </summary>
    public IReadOnlyList<string> RoleCodeList => ParseCodes(AllowedRoleCodes);

    /// <summary>
    /// 绑定权限码列表
    /// </summary>
    public IReadOnlyList<string> PermissionCodeList => ParseCodes(AllowedPermissionCodes);

    /// <summary>
    /// 绑定用户 ID 列表
    /// </summary>
    public IReadOnlyList<int> AdminUserIdList =>
        ParseCodes(AllowedUserIds).Where(z => int.TryParse(z, out _)).Select(z => int.Parse(z)).ToList();

    /// <summary>
    /// 是否至少绑定了一个主体（用户/角色/权限码）
    /// </summary>
    public bool HasAnySubjectBinding =>
        !string.IsNullOrEmpty(AllowedRoleCodes) ||
        !string.IsNullOrEmpty(AllowedPermissionCodes) ||
        !string.IsNullOrEmpty(AllowedUserIds);

    /// <summary>
    /// 当前登录管理员是否命中本策略绑定的用户主体
    /// </summary>
    public bool MatchesAdminUser(int adminUserId) =>
        adminUserId > 0 && AdminUserIdList.Contains(adminUserId);

    public static string NormalizeModuleUid(string moduleUid) => (moduleUid ?? string.Empty).Trim();

    public static string NormalizeFunctionKey(string functionKey) => (functionKey ?? string.Empty).Trim();

    /// <summary>
    /// 策略索引 Key：moduleUid（不区分大小写）|functionKey
    /// </summary>
    public static string GetPolicyKey(string moduleUid, string functionKey) =>
        $"{NormalizeModuleUid(moduleUid)}|{NormalizeFunctionKey(functionKey)}";

    /// <summary>
    /// 解析逗号分隔码列表：去空白、去空项、保序去重
    /// </summary>
    public static IReadOnlyList<string> ParseCodes(string codes)
    {
        if (string.IsNullOrWhiteSpace(codes))
        {
            return Array.Empty<string>();
        }
        return codes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(z => z.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// 规范化逗号分隔码列表
    /// </summary>
    public static string JoinCodes(IReadOnlyList<string> codes)
    {
        if (codes == null || codes.Count == 0)
        {
            return string.Empty;
        }
        return string.Join(',',
            codes
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Select(z => z.Trim())
                .Where(z => z.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 规范化用户 ID 列表（逗号分隔字符串）
    /// </summary>
    public static string JoinUserIds(IReadOnlyList<int> adminUserIds)
    {
        if (adminUserIds == null || adminUserIds.Count == 0)
        {
            return string.Empty;
        }
        return string.Join(',', adminUserIds.Where(z => z > 0).Distinct());
    }
}
