using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// 基类
    /// </summary>
    public class LocalAppServiceBase : AppServiceBase
    {
        public LocalAppServiceBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 获取当前用的Id
        /// </summary>
        /// <returns></returns>
        public int GetCurrentAdminUserInfoId(HttpContext httpContext = null)
        {
            IEnumerable<Claim> claims = httpContext == null ? ServiceProvider.GetService<IHttpContextAccessor>().HttpContext.User.Claims : httpContext.User.Claims;
            bool isConvertSucess = int.TryParse(claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int convertId);
            if (isConvertSucess)
            {
                return convertId;
            }
            return -1;
        }
    }
}
