/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Permission.cshtml.cs
    文件功能描述：Permission.cshtml.cs 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Areas.Admin
{
    [AutoValidateAntiforgeryToken]
    public class PagesRolePermissionModel : BaseAdminPageModel
    {
        private readonly SysRoleService _sysRoleService;
        private readonly SysRolePermissionService _sysPermissionService;

        //private readonly SysRoleMenuService _sysRoleMenuService;
        private readonly SysMenuService _sysMenuService;

        public PagesRolePermissionModel(IServiceProvider serviceProvider, SysMenuService sysMenuService,
            SysRoleService sysRoleService, SysRolePermissionService sysPermissionService)
            : base(serviceProvider)
        {
            CurrentMenu = "Role";
            _sysRoleService = sysRoleService;
            _sysPermissionService = sysPermissionService;
            //_sysRoleMenuService = sysRoleMenuService;
            _sysMenuService = sysMenuService;
        }

        /// <summary>
        /// 
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string RoleId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SysRole SysRoleInfo { get; set; }

        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-grant")]
        public async Task OnGetAsync()
        {
            SysRoleInfo = await _sysRoleService.GetObjectAsync(_ => _.Id == RoleId);
        }

        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-grant")]
        public async Task<IActionResult> OnGetRolePermissionAsync(string roleId)
        {
            var data = await _sysPermissionService.GetFullListAsync(_ => _.RoleId == roleId);
            return Ok(data);
        }

        [Ncf.AreaBase.Admin.Filters.CustomerResource("role-grant")]
        public async Task<IActionResult> OnPostAsync([FromBody] IEnumerable<SysPermissionDto> sysMenuDto)
        {
            if (!sysMenuDto.Any())
            {
                return Ok(false);
            }
            await _sysPermissionService.AddAsync(sysMenuDto);
            return Ok(true);
        }
    }
}
