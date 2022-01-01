using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;

namespace Senparc.Areas.Admin.Authorization
{
    /// <summary>
    /// 权限相关
    /// </summary>
    public class PermissionFilterAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly PermissionRequirement _requirement;
        private readonly IAuthorizationService _authorizationService;

        public PermissionFilterAttribute(IAuthorizationService authorizationService, PermissionRequirement requirement)
        {
            _authorizationService = authorizationService;
            _requirement = requirement;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            //var authorizationService = context.HttpContext.RequestServices.GetService<IAuthorizationService>();
            var result = await _authorizationService.AuthorizeAsync(context.HttpContext.User, null, _requirement);
            if (!result.Succeeded)
            {
                //TODO... ResourceCodes 是否需要暴露出去?
                AppResponseBase<string[]> responseBase = new AppResponseBase<string[]>() { Data = _requirement.ResourceCodes, Success = false, ErrorMessage = "您没有此资源的操作权限。" };
                context.Result = new OkObjectResult(responseBase)
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
            }
        }
    }
}
