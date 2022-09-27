using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Validator;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AuditLog.Controllers;
using Senparc.Xncf.AuditLog.Domain.Services;
using Senparc.Xncf.SystemManager.Domain.Service;


namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class SystemConfig_IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly SystemConfigService _systemConfigService;
        private readonly FullSystemConfigCache _fullSystemConfigCache;
        private readonly AuditLogService _auditLogService;

        //[BindProperty]
        //private FullSystemConfig FullSystemConfig { get; set; }


        public SystemConfig_IndexModel(SystemConfigService systemConfigService, AuditLogService auditLogService, Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
            this._systemConfigService = systemConfigService;
            this._auditLogService = auditLogService;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adminUserInfoName"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        //[Ncf.AreaBase.Admin.Filters.CustomerResource("admin-get-systemconfig")]
        public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        {
            var systemConfig = await _systemConfigService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Ascending);
            return Ok(new { List = systemConfig.AsEnumerable() });
        }

        /// <summary>
        /// 编辑 SystemConfig 信息
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostEditAsync([FromBody]FullSystemConfig  fullSystemConfig)
        {
            var val = this.Validator(fullSystemConfig.SystemName, "系统名称", "SystemName", false);

            if (!ModelState.IsValid)
            {
                return Ok(false, string.Join(",", val.ModelState.Values));
            }

            var systemConfig = await _systemConfigService.GetObjectAsync(z => true);
            //暂时只允许修改 SystemName
            systemConfig.Update(fullSystemConfig.SystemName, systemConfig.MchId, systemConfig.MchKey, systemConfig.TenPayAppId, systemConfig.HideModuleManager);
            await _systemConfigService.SaveObjectAsync(systemConfig);
            _auditLogService.CreateAuditLogInfo(HttpContext.Session.GetString("userName"), IPAddressController.GetClientUserIp(HttpContext.Request.HttpContext), "编辑系统信息", DateTime.Now.ToString());
            base.SetMessager(MessageType.success, $"修改成功！");
            return Ok(new { systemName = systemConfig.SystemName });
        }
    }
}
