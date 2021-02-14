using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Validator;
using Senparc.Ncf.Service;
using Senparc.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class SystemConfig_IndexModel : BaseAdminPageModel
    {
        private readonly SystemConfigService _systemConfigService;
        private readonly TenantInfoService _tenantInfoService;
        private readonly FullSystemConfigCache _fullSystemConfigCache;

        private FullSystemConfig FullSystemConfig { get; set; }


        public SystemConfig_IndexModel(SystemConfigService systemConfigService, TenantInfoService tenantInfoService, FullSystemConfigCache fullSystemConfigCache)
        {
            this._systemConfigService = systemConfigService;
            this._tenantInfoService = tenantInfoService;
            this._fullSystemConfigCache = fullSystemConfigCache;

            FullSystemConfig = _fullSystemConfigCache.Data;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        [HttpPost]
        public async Task<IActionResult> OnPostAsync()
        {
            this.Validator(FullSystemConfig.SystemName, "系统名称", "SystemName", false);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var systemConfig = await _systemConfigService.GetObjectAsync(z => true);
            systemConfig.Update(FullSystemConfig.SystemName, FullSystemConfig.MchId, FullSystemConfig.MchKey, FullSystemConfig.TenPayAppId, FullSystemConfig.HideModuleManager);
            await _systemConfigService.SaveObjectAsync(systemConfig);

            base.SetMessager(MessageType.success, $"修改成功！");
            return RedirectToPage("./Index");
        }
    }
}
