/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml 相关功能实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260705
    修改描述：v0.0.3 新增登录超时配置并补齐多数据库迁移支持

    修改标识：Senparc - 20260705
    修改描述：v0.0.4 新增登录超时配置并补齐多数据库迁移支持
----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.SystemManager.Domain.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class SystemConfig_IndexModel(IServiceProvider serviceProvider,
        SystemConfigService systemConfigService)
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly SystemConfigService _systemConfigService = systemConfigService;

        public async Task<IActionResult> OnGetAsync()
        {
            await Task.CompletedTask;
            return Page();
        }

        //[Ncf.AreaBase.Admin.Filters.CustomerResource("admin-get-systemconfig")]
        public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        {
            var systemConfig = await _systemConfigService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Ascending);
            return Ok(new { TotalCount = systemConfig.TotalCount, PageIndex = systemConfig.PageIndex, List = systemConfig.AsEnumerable() });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] Senparc.Ncf.Core.Models.FullSystemConfig fullSystemConfig)
        {
            if (fullSystemConfig == null)
            {
                return Ok(false, "请求参数不能为空");
            }

            if (string.IsNullOrWhiteSpace(fullSystemConfig.SystemName))
            {
                return Ok(false, "系统名称不能为空");
            }

            var systemConfig = await _systemConfigService.GetObjectAsync(z => true);
            if (systemConfig == null)
            {
                return Ok(false, "系统配置信息不存在");
            }

            systemConfig.Update(fullSystemConfig.SystemName,
                systemConfig.MchId,
                systemConfig.MchKey,
                systemConfig.TenPayAppId,
                systemConfig.HideModuleManager);

            await _systemConfigService.SaveObjectAsync(systemConfig);

            base.SetMessager(MessageType.success, "修改成功");
            return Ok(new
            {
                systemName = systemConfig.SystemName
            });
        }
    }
}
