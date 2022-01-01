using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Dto
{
    public class SysRoleDto
    {
    }

    public class PermissionRequestDto
    {
        /// <summary>
        /// 角色Id
        /// </summary>
        [Required]
        [MaxLength]
        public string RoleId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        [MaxLength]
        public string PermissionId { get; set; }

        public bool IsMenu { get; set; }

        /// <summary>
        /// 角色代码
        /// </summary>
        [Required]
        [MaxLength]
        public string RoleCode { get; set; }

        /// <summary>
        /// 资源（按钮）代码
        /// </summary>
        [Required]
        [MaxLength]
        public string ResourceCode { get; set; }
    }

    public class SysRoleDetailDto
    {
        /// <summary>
        /// 启用状态
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// 角色代码
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime AddTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        public string AdminRemark { get; set; }
        public string Remark { get; set; }
    }

    public class SysRoleListDto
    {
        /// <summary>
        /// 启用状态
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// 角色代码
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime AddTime { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
    }

}
