using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class TenantInfo_IndexModel : BaseAdminPageModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TenantInfoService _tenantInfoService;
        //private readonly FullSystemConfigCache _fullSystemConfigCache;

        //[BindProperty]
        //private FullSystemConfig FullSystemConfig { get; set; }


        public TenantInfo_IndexModel(IServiceProvider serviceProvider, TenantInfoService tenantInfoService
            /*, FullSystemConfigCache fullSystemConfigCache*/)
        {
            this._serviceProvider = serviceProvider;
            this._tenantInfoService = tenantInfoService;
            //this._fullSystemConfigCache = fullSystemConfigCache;

            //FullSystemConfig = _fullSystemConfigCache.Data;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        public async Task<IActionResult> OnGetRequestTenantInfoAsync()
        {
            var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
            return Ok(new
            {
                requestTenantInfo = new
                {
                    requestTenantInfo.Id,
                    requestTenantInfo.Name,
                    requestTenantInfo.TenantKey,
                    BeginTime = requestTenantInfo.BeginTime.ToString("G"),
                },
                tenantRule = SiteConfig.SenparcCoreSetting.TenantRule.ToString(),
                SiteConfig.SenparcCoreSetting.EnableMultiTenant
            }); ;
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
            var tenantInfo = await _tenantInfoService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Ascending);
            return Ok(new { List = tenantInfo.AsEnumerable() });
        }

        /// <summary>
        /// Handler=Save
        /// 保存
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [CustomerResource("admin-add", "admin-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] CreateOrUpdate_TenantInfoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(false);
            }
            bool isNameExists = await this._tenantInfoService.CheckNameExisted(dto.Id, dto.Name);
            if (isNameExists)
            {
                return Ok(false, $"{dto.Name} 名称已存在");
            }

            bool isTenantKeyExists = await this._tenantInfoService.CheckTenantKeyExisted(dto.Id, dto.TenantKey);
            if (isTenantKeyExists)
            {
                return Ok(false, $"{dto.TenantKey} 匹配规则已存在");
            }

            try
            {
                await _tenantInfoService.CreateOrUpdateTenantInfoAsync(dto);
                return Ok(true, $"{(dto.Id > 0 ? "编辑" : "新增")}租户成功！");
            }
            catch (Exception ex)
            {
                return Ok(false, ex.Message);
            }

        }
    }
}
