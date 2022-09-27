using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Xncf.AuditLog.Controllers;
using Senparc.Xncf.AuditLog.Domain.Services;
using Senparc.Xncf.SystemCore.Domain.Database;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken]
    public class RoleEditModel : BaseAdminPageModel
    {
        private readonly SysRoleService _sysRoleService;
        private readonly AuditLogService _auditLogService;

        public RoleEditModel(SysRoleService sysRoleService, AuditLogService auditLogService)
        {
            CurrentMenu = "Role";
            _sysRoleService = sysRoleService;
            this._auditLogService = auditLogService;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        [BindProperty]
        public SysRoleDto SysRoleDto { get; set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                var entity = await _sysRoleService.GetObjectAsync(_ => _.Id == Id);
                SysRoleDto = _sysRoleService.Mapper.Map<SysRoleDto>(entity);
            }
        }

        /// <summary>
        /// Handler=Detail
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-edit")]
        public async Task<IActionResult> OnGetDetailAsync(string id)
        {
            var entity = await _sysRoleService.GetObjectAsync(_ => _.Id == id);
            return Ok(entity);
        }

        /// <summary>
        /// handler=SelectItems
        /// </summary>
        /// <param name="senparcEntities"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetSelectItemsAsync([FromServices] /*SenparcEntities*/ BasePoolEntities senparcEntities)
        {
            var list = await senparcEntities.Set<SysRole>().Where(_ => _.Enabled)
                .Select(_ => new SelectListItem() { Value = _.Id, Text = _.RoleName })
                .ToListAsync();
            return Ok(list);
        }

        /// <summary>
        /// Handler=Save
        /// </summary>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-add", "role-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] SysRoleDto sysRoleDto)
        {
            await _sysRoleService.CreateOrUpdateAsync(sysRoleDto);
            _auditLogService.CreateAuditLogInfo(HttpContext.Session.GetString("userName"), IPAddressController.GetClientUserIp(HttpContext.Request.HttpContext), "编辑/添加角色信息", DateTime.Now.ToString());
            return Ok(true);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //if (!string.IsNullOrEmpty(Id))
            //{
            //    var entity = await _sysRoleService.GetObjectAsync(_ => _.Id == Id);
            //    SysRoleDto = _sysRoleService.Mapper.Map<SysRoleDto>(entity);
            //}
            await _sysRoleService.CreateOrUpdateAsync(SysRoleDto);
            return RedirectToPage("./Index");
        }
    }
}