using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysMenu_Request
    {

    }

    /// <summary>
    /// 菜单创建请求
    /// </summary>
    public class SysMenu_CreateOrUpdateRequest
    {
        [MaxLength(50)]
        public string Id { get; set; }

        [MaxLength(150)]
        [Required]
        public string MenuName { get; set; }

        /// <summary>
        /// 父菜单
        /// </summary>
        [MaxLength(50)]
        public string ParentId { get; set; }

        [MaxLength(350)]
        public string Url { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool Visible { get; set; }

        public string ResourceCode { get; set; }

        public bool IsLocked { get; set; }

        public Ncf.Core.Models.DataBaseModel.MenuType MenuType { get; set; }
    }
}
