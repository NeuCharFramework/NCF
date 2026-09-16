/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotGlobalFunctionService.cs
    文件功能描述：全局 NeuCharPivot Function 映射、访问控制与执行入口

    修改标识：Senparc - 20260829
    修改描述：v0.7.0 新增 NeuCharPivot 全局浮动调用与工作流分析管理能力

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed record NeuCharGlobalFunctionResolution(
    NeuCharFunctionDescriptor Descriptor,
    string ErrorMessage)
{
    public bool Success => Descriptor != null && string.IsNullOrWhiteSpace(ErrorMessage);
}

public sealed class NeuCharPivotGlobalAccessService
{
    private readonly IAdminWorkContextProvider _workContextProvider;
    private readonly ICheckPermission _checkPermission;

    public NeuCharPivotGlobalAccessService(
        IAdminWorkContextProvider workContextProvider,
        ICheckPermission checkPermission)
    {
        _workContextProvider = workContextProvider;
        _checkPermission = checkPermission;
    }

    public async Task<string> GetDenialReasonAsync(NeuCharFunctionDescriptor descriptor)
    {
        var context = _workContextProvider.GetAdminWorkContext();
        if (context == null || context.AdminUserId <= 0)
        {
            return "请先登录后台管理员账号。";
        }

        var requiredRoles = descriptor.GlobalPivotRoleCodes ?? Array.Empty<string>();
        var requiredPermissions = descriptor.GlobalPivotPermissionCodes ?? Array.Empty<string>();
        if (requiredRoles.Count == 0 && requiredPermissions.Count == 0)
        {
            return null;
        }

        if (RoleMatches(context, requiredRoles))
        {
            return null;
        }

        if (requiredPermissions.Count > 0 &&
            await _checkPermission.HasPermissionAsync(
                requiredPermissions.ToArray(),
                context.AdminUserId).ConfigureAwait(false))
        {
            return null;
        }

        return "当前账号没有访问该全局 Function 的角色或权限。";
    }

    public static bool RoleMatches(
        AdminWorkContext context,
        IEnumerable<string> requiredRoles)
    {
        var required = requiredRoles?
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (required.Count == 0)
        {
            return false;
        }

        return (context?.RoleCodes ?? Array.Empty<string>())
            .Any(role => required.Contains(role));
    }
}

/// <summary>
/// 全局浮动 Function 的唯一服务端入口。
/// Function 必须显式声明 AllowGlobalPivot；访问规则为空时仅要求当前后台管理员已登录。
/// </summary>
public sealed class NeuCharPivotGlobalFunctionService
{
    private readonly NeuCharFunctionService _functionService;
    private readonly NeuCharPivotGlobalAccessService _accessService;

    public NeuCharPivotGlobalFunctionService(
        NeuCharFunctionService functionService,
        NeuCharPivotGlobalAccessService accessService)
    {
        _functionService = functionService;
        _accessService = accessService;
    }

    public async Task<NeuCharGlobalFunctionResolution> ResolveAsync(
        string moduleUid,
        string functionKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(moduleUid) || string.IsNullOrWhiteSpace(functionKey))
        {
            return Denied("模块 UID 和 Function Key 不能为空。");
        }

        var descriptor = (await _functionService.GetCatalogAsync(
                moduleUid,
                true,
                cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault(item => MatchesFunctionKey(item, functionKey));

        if (descriptor == null)
        {
            return Denied("Function 不存在、未加载或已在模块更新后移除。");
        }

        if (!descriptor.AllowGlobalPivot)
        {
            return Denied("该 Function 未声明为全局 NeuCharPivot 映射。");
        }

        var accessError = await _accessService.GetDenialReasonAsync(descriptor).ConfigureAwait(false);
        return accessError == null
            ? new NeuCharGlobalFunctionResolution(descriptor, null)
            : Denied(accessError);
    }

    public async Task<NeuCharFunctionExecutionResult> ExecuteAsync(
        string moduleUid,
        string functionKey,
        string parametersJson,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveAsync(moduleUid, functionKey, cancellationToken)
            .ConfigureAwait(false);
        if (!resolution.Success)
        {
            return new NeuCharFunctionExecutionResult(
                false,
                null,
                resolution.ErrorMessage,
                null);
        }

        return await _functionService.ExecuteAsync(
                resolution.Descriptor.ModuleUid,
                resolution.Descriptor.FunctionKey,
                parametersJson,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static NeuCharGlobalFunctionResolution Denied(string message) =>
        new(null, message);

    public static bool MatchesFunctionKey(
        NeuCharFunctionDescriptor descriptor,
        string functionKey)
    {
        return descriptor != null &&
            !string.IsNullOrWhiteSpace(functionKey) &&
            (string.Equals(descriptor.FunctionKey, functionKey, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(descriptor.MethodName, functionKey, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(descriptor.Name, functionKey, StringComparison.OrdinalIgnoreCase));
    }
}
