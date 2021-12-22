using Microsoft.AspNetCore.Mvc;

namespace Senparc.Areas.Admin.Authorization
{
    /// <summary>
    /// 权限特性
    /// </summary>
    public class PermissionAttribute : TypeFilterAttribute
    {
        //public string[] Codes { get; set; }

        /// <summary>
        /// code里面不能存在英文逗号 TODO
        /// </summary>
        /// <param name="codes">多个code 英文逗号分割</param>
        public PermissionAttribute(string codes)
            : base(typeof(PermissionFilterAttribute))
        {
            //Arguments = new[] { new PermissionRequirement(Codes) };
            Arguments = new[] { new PermissionRequirement(codes.Split(",")) };
        }
    }
}
