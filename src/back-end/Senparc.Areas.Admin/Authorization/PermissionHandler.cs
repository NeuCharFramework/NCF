using Microsoft.AspNetCore.Authorization;
using Senparc.Areas.Admin.Domain.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Authorization
{
    /// <summary>
    /// 校验
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly SysRolePermissionService _sysRolePermissionService;

        public PermissionHandler(SysRolePermissionService sysRolePermissionService)
        {
            _sysRolePermissionService = sysRolePermissionService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (requirement.ResourceCodes.Length == 1 && requirement.ResourceCodes.Contains(PermissionRequirement.All))
            {
                // 有且仅有一个*
                context.Succeed(requirement);
                return;
            }
            bool isConvertSucess = int.TryParse(context.User.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int adminUserInfoId);
            bool hasPermission = await _sysRolePermissionService.HasPermissionAsync(requirement.ResourceCodes, adminUserInfoId); // 当前用户是否有权限
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}
