using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Senparc.Mvc
{
    public class AjaxMethodAttribute : ActionMethodSelectorAttribute
    {
        public AjaxMethodAttribute()
        {
        }

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            return routeContext.HttpContext.Request.IsAjaxRequest();

        }
    }
}
