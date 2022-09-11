using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AuditLog.Controllers;
using Senparc.Xncf.AuditLog.Domain.Services;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken]
    public class RoleIndexModel : BaseAdminPageModel
    {
        private readonly SysRoleService _sysRoleService;

        private readonly AuditLogService _auditLogService;

        public RoleIndexModel(SysRoleService sysRoleService, AuditLogService auditLogService)
        {
            CurrentMenu = "Role";
            this._sysRoleService = sysRoleService;
            this._auditLogService = auditLogService;
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
        public IActionResult OnPostDelete([FromBody]string[] ids)
        {
            foreach (var id in ids)
            {
                _sysRoleService.DeleteObject(_ => _.Id == id);
                _auditLogService.CreateAuditLogInfo(HttpContext.Session.GetString("userName"), IPAddressController.GetClientUserIp(HttpContext.Request.HttpContext), "É¾³ý½ÇÉ«ÐÅÏ¢", DateTime.Now.ToString());
            }
            return Ok(ids.Length);
        }
    }
}