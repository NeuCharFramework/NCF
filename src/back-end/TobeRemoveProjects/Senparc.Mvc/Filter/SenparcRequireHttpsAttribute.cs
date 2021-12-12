using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Ncf.Core.Config;

namespace Senparc.Mvc.Filter
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
          Justification = "Unsealed so that subclassed types can set properties in the default constructor or override our behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class SenparcRequireHttpsAttribute: RequireHttpsAttribute
    {
        public override void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (SiteConfig.IsTestSite)
            {
                return;//如果是测试站，则忽略验证
            }

            base.OnAuthorization(filterContext);
        }
    }
}
