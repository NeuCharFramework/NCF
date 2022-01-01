using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysRole_Request
    {
    }

    public class SysRole_GetListRequest
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }

        public string Kw { get; set; }
    }

    public class SysRole_CreateOrUpdateRequest
    {
        [MaxLength(50)]
        public string Id
        {
            get;
            set;
        }

        [MaxLength(50)]
        [Required]
        public string RoleName
        {
            get;
            set;
        }

        [MaxLength(50)]
        [Required]
        public string RoleCode
        {
            get;
            set;
        }

        public bool Enabled
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 授权操作
    /// </summary>
    public class SysRole_AddPermissionRequest
    {
        public IEnumerable<Domain.Dto.PermissionRequestDto> RequestDtos { get; set; }
    }
}
