using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysMenu_Response
    {

    }

    /// <summary>
    /// 菜单创建响应
    /// </summary>
    public class SysMenu_CreateOrUpdateResponse
    {
        /// <summary>
        /// 创建后的主键编号
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// 菜单树 不包含按钮
    /// </summary>
    public class SysMenu_MenuTreeResponse
    {
        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuTreeItemDto> Items { get; set; }
    }

    /// <summary>
    /// 菜单列表项目
    /// </summary>
    public class SysMenu_MenusResponse
    {
        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuDto> Items { get; set; }
    }

    /// <summary>
    /// 菜单详情
    /// </summary>
    public class SysMenu_GetMenuResponse
    {
        public Ncf.Core.Models.DataBaseModel.SysMenuDto Item { get; set; }
    }
}
