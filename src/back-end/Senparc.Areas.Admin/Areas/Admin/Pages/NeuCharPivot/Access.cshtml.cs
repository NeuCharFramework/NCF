/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Access.cshtml.cs
    文件功能描述：Function 全局 Provit 访问控制管理页：
    数据库访问策略映射（绑定用户/角色/权限码）的可视化维护：
    完整信息展示（代码基线 + 数据库策略 + 生效策略）、单条与批量设置、手动清除。
    策略行冗余保留：模块清除后不删除，模块重装后自动继续生效。

    创建标识：Senparc - 20260917
    修改描述：v0.9.1 新增 Function 全局 Provit 数据库访问策略映射

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
public class AccessModel(
    IServiceProvider serviceProvider,
    NeuCharFunctionProvitAccessService accessPolicyService,
    NeuCharFunctionService functionService,
    AdminUserInfoService adminUserInfoService,
    SysRoleService sysRoleService) : BaseAdminPageModel(serviceProvider)
{
    private readonly NeuCharFunctionProvitAccessService _accessPolicyService = accessPolicyService;
    private readonly NeuCharFunctionService _functionService = functionService;
    private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;
    private readonly SysRoleService _sysRoleService = sysRoleService;

    /// <summary>
    /// 默认 GET：渲染访问控制页（JSON 数据由 handler=List/Users/Roles 提供）
    /// </summary>
    public IActionResult OnGet()
    {
        return Page();
    }

    /// <summary>
    /// 全量列表：所有 XNCF 模块 Function 的代码基线 + 数据库策略 + 生效策略，
    /// 以及“孤儿策略”（模块/Function 已不存在，策略冗余保留待重装生效）。
    /// </summary>
    public async Task<IActionResult> OnGetListAsync()
    {
        var catalog = await _functionService.GetCatalogAsync(null, false, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        var policies = _accessPolicyService.GetAllPolicies();
        var policyIndex = policies.ToDictionary(
            z => NeuCharFunctionProvitAccess.GetPolicyKey(z.ModuleUid, z.FunctionKey),
            StringComparer.OrdinalIgnoreCase);

        var items = new List<object>();
        var matchedPolicyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in catalog)
        {
            var policyKey = NeuCharFunctionProvitAccess.GetPolicyKey(
                descriptor.ModuleUid,
                descriptor.FunctionKey);
            policyIndex.TryGetValue(policyKey, out var policy);
            if (policy != null)
            {
                matchedPolicyKeys.Add(policyKey);
            }

            items.Add(BuildFunctionItem(
                moduleUid: descriptor.ModuleUid,
                moduleName: descriptor.ModuleName,
                moduleVersion: descriptor.ModuleVersion,
                moduleAvailable: descriptor.ModuleAvailable,
                functionKey: descriptor.FunctionKey,
                functionName: descriptor.Name,
                functionDescription: descriptor.Description,
                codeAllowGlobalPivot: descriptor.AllowGlobalPivot,
                codeRoleCodes: descriptor.GlobalPivotRoleCodes,
                codePermissionCodes: descriptor.GlobalPivotPermissionCodes,
                policy: policy,
                orphan: false));
        }

        // 孤儿策略：模块被清除或 Function 已移除，策略冗余保留
        var orphans = policies
            .Where(z => !matchedPolicyKeys.Contains(
                NeuCharFunctionProvitAccess.GetPolicyKey(z.ModuleUid, z.FunctionKey)))
            .OrderBy(z => z.ModuleUid)
            .ThenBy(z => z.FunctionKey)
            .ToList();
        foreach (var orphan in orphans)
        {
            items.Add(BuildFunctionItem(
                moduleUid: orphan.ModuleUid,
                moduleName: null,
                moduleVersion: null,
                moduleAvailable: false,
                functionKey: orphan.FunctionKey,
                functionName: null,
                functionDescription: null,
                codeAllowGlobalPivot: null,
                codeRoleCodes: null,
                codePermissionCodes: null,
                policy: orphan,
                orphan: true));
        }

        return Ok(new
        {
            items,
            total = items.Count,
            policyCount = policies.Count,
            orphanCount = orphans.Count
        });
    }

    /// <summary>
    /// 后台管理员账号列表（绑定用户主体用）
    /// </summary>
    public async Task<IActionResult> OnGetUsersAsync()
    {
        var users = await _adminUserInfoService.GetFullListAsync(
            z => !z.Flag, z => z.Id, OrderingType.Ascending).ConfigureAwait(false);
        return Ok(users
            .Select(z => new
            {
                z.Id,
                z.UserName,
                z.RealName,
                z.Note
            }));
    }

    /// <summary>
    /// 系统角色列表（绑定角色主体用）
    /// </summary>
    public async Task<IActionResult> OnGetRolesAsync()
    {
        var roles = await _sysRoleService.GetFullListAsync(
            z => z.Enabled, z => z.RoleCode, OrderingType.Ascending).ConfigureAwait(false);
        return Ok(roles
            .Select(z => new
            {
                z.RoleCode,
                z.RoleName
            }));
    }

    /// <summary>
    /// 保存单条策略（新增或更新）
    /// </summary>
    public async Task<IActionResult> OnPostSaveAsync([FromBody] PolicySaveRequest request)
    {
        try
        {
            var policy = await _accessPolicyService.UpsertPolicyAsync(
                    ToInput(request),
                    HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return Ok(ToPolicyDto(policy));
        }
        catch (Exception ex)
        {
            return Ok(false, ex.Message);
        }
    }

    /// <summary>
    /// 手动清除单条策略（物理删除；模块清除不会触发，策略冗余保留）
    /// </summary>
    public async Task<IActionResult> OnPostClearAsync([FromBody] PolicyKeyRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.ModuleUid) ||
            string.IsNullOrWhiteSpace(request.FunctionKey))
        {
            return Ok(false, "参数无效");
        }
        var deleted = await _accessPolicyService.DeletePolicyAsync(
                request.ModuleUid,
                request.FunctionKey,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (deleted)
        {
            return Ok(new { deleted });
        }
        return Ok(false, "策略不存在");
    }

    /// <summary>
    /// 批量设置 / 批量清除。
    /// operation：apply-open / apply-restrict / apply-deny / apply-inherit / clear
    /// </summary>
    public async Task<IActionResult> OnPostBatchAsync([FromBody] PolicyBatchRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Operation) ||
            request.Items == null ||
            request.Items.Count == 0)
        {
            return Ok(false, "请选择要操作的 Function");
        }

        try
        {
            switch (request.Operation)
            {
                case "clear":
                    var cleared = await _accessPolicyService.BatchDeleteAsync(
                            request.Items.Select(z => (z.ModuleUid, z.FunctionKey)),
                            HttpContext.RequestAborted)
                        .ConfigureAwait(false);
                    return Ok(new { affected = cleared });

                case "apply-open":
                case "apply-restrict":
                case "apply-deny":
                case "apply-inherit":
                    var mode = request.Operation switch
                    {
                        "apply-open" => NeuCharFunctionProvitAccess.AccessModeOpen,
                        "apply-restrict" => NeuCharFunctionProvitAccess.AccessModeRestricted,
                        "apply-deny" => NeuCharFunctionProvitAccess.AccessModeDeny,
                        _ => NeuCharFunctionProvitAccess.AccessModeInherit
                    };
                    var inputs = request.Items
                        .Where(z => !string.IsNullOrWhiteSpace(z.ModuleUid)
                            && !string.IsNullOrWhiteSpace(z.FunctionKey))
                        .Select(z => new NeuCharFunctionProvitAccessInput
                        {
                            ModuleUid = z.ModuleUid,
                            FunctionKey = z.FunctionKey,
                            AccessMode = mode,
                            // 批量“受限”时可附带统一主体绑定；其余模式忽略绑定
                            AllowedRoleCodes = mode == NeuCharFunctionProvitAccess.AccessModeRestricted
                                ? request.AllowedRoleCodes ?? new List<string>()
                                : new List<string>(),
                            AllowedPermissionCodes = mode == NeuCharFunctionProvitAccess.AccessModeRestricted
                                ? request.AllowedPermissionCodes ?? new List<string>()
                                : new List<string>(),
                            AllowedUserIds = mode == NeuCharFunctionProvitAccess.AccessModeRestricted
                                ? request.AllowedUserIds ?? new List<int>()
                                : new List<int>(),
                            Remark = request.Remark
                        })
                        .ToList();
                    if (inputs.Count == 0)
                    {
                        return Ok(false, "没有有效的操作对象");
                    }
                    var applied = await _accessPolicyService.BatchUpsertAsync(
                            inputs,
                            HttpContext.RequestAborted)
                        .ConfigureAwait(false);
                    return Ok(new { affected = applied });

                default:
                    return Ok(false, $"不支持的操作：{request.Operation}");
            }
        }
        catch (Exception ex)
        {
            return Ok(false, ex.Message);
        }
    }

    #region 私有辅助

    private static NeuCharFunctionProvitAccessInput ToInput(PolicySaveRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("请求无效");
        }
        return new NeuCharFunctionProvitAccessInput
        {
            ModuleUid = request.ModuleUid,
            FunctionKey = request.FunctionKey,
            AccessMode = request.AccessMode,
            AllowedRoleCodes = request.AllowedRoleCodes ?? new List<string>(),
            AllowedPermissionCodes = request.AllowedPermissionCodes ?? new List<string>(),
            AllowedUserIds = request.AllowedUserIds ?? new List<int>(),
            Remark = request.Remark
        };
    }

    private static object ToPolicyDto(NeuCharFunctionProvitAccess policy)
    {
        return new
        {
            policy.Id,
            policy.ModuleUid,
            policy.FunctionKey,
            policy.AccessMode,
            roleCodes = policy.RoleCodeList,
            permissionCodes = policy.PermissionCodeList,
            userIds = policy.AdminUserIdList,
            policy.Remark,
            policy.AddTime,
            policy.LastUpdateTime
        };
    }

    private static object BuildFunctionItem(
        string moduleUid,
        string moduleName,
        string moduleVersion,
        bool? moduleAvailable,
        string functionKey,
        string functionName,
        string functionDescription,
        bool? codeAllowGlobalPivot,
        IReadOnlyList<string> codeRoleCodes,
        IReadOnlyList<string> codePermissionCodes,
        NeuCharFunctionProvitAccess policy,
        bool orphan)
    {
        // 生效策略：数据库策略（非继承）优先，否则代码基线
        var dbActive = policy != null &&
            policy.AccessMode != NeuCharFunctionProvitAccess.AccessModeInherit;
        var exposureAllowed = NeuCharPivotGlobalAccessService.ResolveExposureAllowed(
            codeAllowGlobalPivot == true,
            policy);
        string effectiveMode;
        string effectiveSource;
        if (orphan)
        {
            // 孤儿策略：Function 不存在时策略暂不生效，模块重装后按数据库策略生效
            effectiveMode = "pending";
            effectiveSource = "db";
        }
        else if (dbActive)
        {
            effectiveMode = policy.AccessMode switch
            {
                NeuCharFunctionProvitAccess.AccessModeOpen => "open",
                NeuCharFunctionProvitAccess.AccessModeRestricted => "restricted",
                NeuCharFunctionProvitAccess.AccessModeDeny => "deny",
                _ => "inherit"
            };
            effectiveSource = "db";
        }
        else
        {
            effectiveMode = codeAllowGlobalPivot == false
                ? "deny"
                : (codeRoleCodes?.Count > 0 || codePermissionCodes?.Count > 0)
                    ? "restricted"
                    : "open";
            effectiveSource = "code";
        }

        return new
        {
            moduleUid,
            moduleName,
            moduleVersion,
            moduleAvailable,
            functionKey,
            functionName,
            functionDescription,
            orphan,
            code = new
            {
                allowGlobalPivot = codeAllowGlobalPivot,
                roleCodes = codeRoleCodes?.ToList() ?? new List<string>(),
                permissionCodes = codePermissionCodes?.ToList() ?? new List<string>()
            },
            db = policy == null ? null : new
            {
                policy.Id,
                policy.ModuleUid,
                policy.FunctionKey,
                policy.AccessMode,
                roleCodes = policy.RoleCodeList,
                permissionCodes = policy.PermissionCodeList,
                userIds = policy.AdminUserIdList,
                policy.Remark,
                policy.AddTime,
                policy.LastUpdateTime
            },
            effective = new
            {
                exposureAllowed,
                effectiveMode,
                effectiveSource
            }
        };
    }

    #endregion

    #region 请求模型

    public sealed class PolicyKeyRequest
    {
        public string ModuleUid { get; set; }
        public string FunctionKey { get; set; }
    }

    public sealed class PolicySaveRequest
    {
        public string ModuleUid { get; set; }
        public string FunctionKey { get; set; }
        public int AccessMode { get; set; } = NeuCharFunctionProvitAccess.AccessModeInherit;
        public List<string> AllowedRoleCodes { get; set; } = new();
        public List<string> AllowedPermissionCodes { get; set; } = new();
        public List<int> AllowedUserIds { get; set; } = new();
        public string Remark { get; set; }
    }

    public sealed class PolicyBatchRequest
    {
        public string Operation { get; set; }
        public List<PolicyKeyRequest> Items { get; set; } = new();
        public List<string> AllowedRoleCodes { get; set; } = new();
        public List<string> AllowedPermissionCodes { get; set; } = new();
        public List<int> AllowedUserIds { get; set; } = new();
        public string Remark { get; set; }
    }

    #endregion
}
