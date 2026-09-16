/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Function.cshtml.cs
    文件功能描述：全局 NeuCharPivot Function 浮动调用接口

    修改标识：Senparc - 20260829
    修改描述：v0.7.0 新增 NeuCharPivot 全局浮动调用与工作流分析管理能力

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages.NeuCharPivot;

[IgnoreAuth]
[AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public class FunctionModel(
    IServiceProvider serviceProvider,
    NeuCharPivotGlobalFunctionService globalFunctionService) : BaseAdminPageModel(serviceProvider)
{
    private readonly NeuCharPivotGlobalFunctionService _globalFunctionService = globalFunctionService;

    public async Task<IActionResult> OnGetDescribeAsync(
        [FromQuery] string moduleUid,
        [FromQuery] string functionKey)
    {
        var resolution = await _globalFunctionService.ResolveAsync(
                moduleUid,
                functionKey,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!resolution.Success)
        {
            return StatusCode(403, resolution.ErrorMessage);
        }

        var descriptor = resolution.Descriptor;
        return Ok(new
        {
            descriptor.ModuleUid,
            descriptor.ModuleName,
            descriptor.ModuleVersion,
            descriptor.FunctionKey,
            descriptor.Name,
            descriptor.Description,
            parameters = descriptor.Parameters,
            access = new
            {
                enabled = descriptor.AllowGlobalPivot,
                roleCodes = descriptor.GlobalPivotRoleCodes,
                permissionCodes = descriptor.GlobalPivotPermissionCodes
            }
        });
    }

    public async Task<IActionResult> OnPostRunAsync([FromBody] GlobalFunctionRunRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.ModuleUid) ||
            string.IsNullOrWhiteSpace(request.FunctionKey))
        {
            return BadRequest("全局 Function 请求无效。");
        }

        if (request.ParametersJson?.Length > 1_000_000)
        {
            return BadRequest("Function 参数不能超过 1000000 个字符。");
        }

        var result = await _globalFunctionService.ExecuteAsync(
                request.ModuleUid,
                request.FunctionKey,
                request.ParametersJson,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return Ok(result);
    }

    public sealed class GlobalFunctionRunRequest
    {
        public string ModuleUid { get; set; }
        public string FunctionKey { get; set; }
        public string ParametersJson { get; set; } = "{}";
    }
}
