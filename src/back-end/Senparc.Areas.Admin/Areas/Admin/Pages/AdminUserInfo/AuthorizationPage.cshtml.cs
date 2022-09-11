using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Xncf.AuditLog.Controllers;
using Senparc.Xncf.AuditLog.Domain.Services;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class AdminUserInfoAuthorizationPageModel : BaseAdminPageModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SysRoleAdminUserInfoService sysRoleAdminUserInfoService;
        private readonly AuditLogService _auditLogService;

        public AdminUserInfoAuthorizationPageModel(IServiceProvider _serviceProvider, SysRoleAdminUserInfoService sysRoleAdminUserInfoService,AuditLogService auditLogService)
        {
            this._serviceProvider = _serviceProvider;
            this.sysRoleAdminUserInfoService = sysRoleAdminUserInfoService;
            this._auditLogService = auditLogService;
        }

        public IEnumerable<SysRoleAdminUserInfoDto> RoleAdminUserInfoDtos { get; set; }

        public async Task OnGetAsync(int accountId)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取用户的角色
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-grant")]
        public async Task<IActionResult> OnGetDetailAsync(int accountId)
        {
            IEnumerable<SysRoleAdminUserInfo> sysRoleAdminUserInfos = await sysRoleAdminUserInfoService.GetFullListAsync(_ => _.AccountId == accountId);
            return Ok(sysRoleAdminUserInfos);
        }

        [Ncf.AreaBase.Admin.Filters.CustomerResource("admin-grant")]
        public async Task<IActionResult> OnPostAsync([FromBody] GrantRoleDto grantRoleDto)
        {
            await sysRoleAdminUserInfoService.AddAsync(grantRoleDto.RoleIds, grantRoleDto.AccountId);
            _auditLogService.CreateAuditLogInfo(HttpContext.Session.GetString("userName"), IPAddressController.GetClientUserIp(HttpContext.Request.HttpContext), "设置管理员角色", DateTime.Now.ToString());
            return Ok(true);
        }
    }

    public class GrantRoleDto
    {
        public IEnumerable<string> RoleIds { get; set; }

        public int AccountId { get; set; }
    }
}