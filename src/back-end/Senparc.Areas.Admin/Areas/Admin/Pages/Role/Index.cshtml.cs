/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml.cs 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AutoValidateAntiforgeryToken]
    public class RoleIndexModel : BaseAdminPageModel
    {
        private readonly SysRoleService _sysRoleService;

        public RoleIndexModel(IServiceProvider serviceProvider, SysRoleService sysRoleService)
            : base(serviceProvider)
        {
            CurrentMenu = "Role";
            this._sysRoleService = sysRoleService;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 
        /// </summary>
        public PagedList<SysRole> SysRoles { get; set; }

        public void OnGet()
        {
            //SysRoles = new ;// await _sysRoleService.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
        }

        /// <summary>
        /// Handler=List
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="orderField">XXX DESC,YYY ASC</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-search")]
        public async Task<IActionResult> OnGetListAsync(string roleName, string orderField, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<SysRole>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(roleName), _ => _.RoleName.Contains(roleName));
            var where = seh.BuildWhereExpression();
            var admins = await _sysRoleService.GetObjectListAsync(pageIndex, pageSize, where, orderField);

            //AdminUserInfoList = admins;
            return Ok(new
            {
                admins.TotalCount,
                admins.PageIndex,
                List =
                admins.Select(_ => new { _.Id, _.LastUpdateTime, _.Remark, _.RoleName, _.RoleCode, _.AddTime, _.AdminRemark, _.Enabled })
            });
        }

        //public IActionResult OnPostDelete(string[] ids)
        //{
        //    foreach (var id in ids)
        //    {
        //        _sysRoleService.DeleteObject(_ => _.Id == id);
        //    }

        //    return RedirectToPage("./Index");
        //}

        /// <summary>
        /// Handler=Delete
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-delete")]
        public IActionResult OnPostDelete([FromBody] string[] ids)
        {
            foreach (var id in ids)
            {
                _sysRoleService.DeleteObject(_ => _.Id == id);
            }
            return Ok(ids.Length);
        }
    }
}
