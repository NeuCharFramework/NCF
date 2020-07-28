using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.TagHelpers
{
    // You may need to install the Microsoft.AspNetCore.Razor.Runtime package into your project
    [HtmlTargetElement("a", Attributes = nameof(PermissionCode))]
    public class AuthorizationMenuTagHelper : TagHelper
    {
        public string PermissionCode { get; set; }
        private readonly IServiceProvider _serviceProvider;
        private readonly SysPermissionService _sysPermissionService;
        //private readonly Core.WorkContext.Provider.IAdminWorkContextProvider _adminWorkContextProvider;
        //private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;

        public AuthorizationMenuTagHelper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _sysPermissionService = _serviceProvider.GetService<SysPermissionService>();
            _actionContextAccessor = _serviceProvider.GetService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor>();
            //_adminWorkContextProvider = _serviceProvider.GetService<Core.WorkContext.Provider.IAdminWorkContextProvider>();
            //_httpContextAccessor = _serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            bool hasPageRoute = _actionContextAccessor.ActionContext.RouteData.Values.TryGetValue("page", out object page);
            bool hasAreaRoute = _actionContextAccessor.ActionContext.RouteData.Values.TryGetValue("area", out object area);
            bool hasAttribute = output.Attributes.TryGetAttribute(nameof(PermissionCode), out TagHelperAttribute tagHelperAttribute);
            bool hasRight = hasPageRoute && hasAreaRoute && hasAttribute;
            if (hasRight)
            {
                PermissionCode = tagHelperAttribute.Value.ToString();
                hasRight = await _sysPermissionService.HasPermissionByButtonCodeAsync(PermissionCode, string.Concat("/", area, page));
            }
            if (!hasRight)
            {
                output.SuppressOutput();
                return;
            }
            await base.ProcessAsync(context, output);
        }
    }
}
