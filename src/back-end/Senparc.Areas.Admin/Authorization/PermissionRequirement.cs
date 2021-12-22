using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Areas.Admin.Authorization
{
    /// <summary>
    /// 资源
    /// </summary>
    public class PermissionRequirement: IAuthorizationRequirement
    {

        /// <summary>
        /// 登录后即可访问
        /// </summary>
        public const string All = "*";

        /// <summary>
        /// 资源code
        /// </summary>
        public string[] ResourceCodes { get; set; }

        public PermissionRequirement()
        {
            this.ResourceCodes = new string[] { All };
        }

        public PermissionRequirement(string[] resourceCodes)
        {
            ResourceCodes = resourceCodes.Where(_ => !string.IsNullOrEmpty(_?.Trim())).Select(_ => _.Trim()).ToArray(); // 去前后空格
        }
    }
}
