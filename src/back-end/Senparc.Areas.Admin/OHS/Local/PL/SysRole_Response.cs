using Senparc.Areas.Admin.Domain.Dto;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysRole_Response
    {
    }

    /// <summary>
    /// 角色列表
    /// </summary>
    public class SysRole_GetListResponse
    {
        public IEnumerable<SysRoleListDto> List { get; set; }

        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 详情
    /// </summary>
    public class SysRole_GetSingleResponse
    {
        public SysRoleDetailDto Detail { get; set; }
    }

    /// <summary>
    /// 角色更新
    /// </summary>
    public class SysRole_CreateOrUpdateResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// 角色下面的所有权限
    /// </summary>
    public class SysRole_PermissionsResponse
    {
        public IEnumerable<string> PermissionId { get; set; }
    }
}
