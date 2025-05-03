using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Pipelines.Sockets.Unofficial.Buffers;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class TenantInfo_IndexModel(IServiceProvider serviceProvider, TenantInfoService tenantInfoService,
            InstallerService installerService,
            /*, FullSystemConfigCache fullSystemConfigCache*/ AdminUserInfoService adminUserInfoService)
            : BaseAdminPageModel(serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly TenantInfoService _tenantInfoService = tenantInfoService;
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;
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
            return Ok(new { 
                List = tenantInfo.AsEnumerable(),
                TotalCount = tenantInfo.TotalCount,
                PageIndex = tenantInfo.PageIndex
            });
        }

        /// <summary>
        /// Handler=Save
        /// 新增、编辑租户
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
                return Ok(false, $"{dto.Name} 租户名称已存在");
            }

            bool isTenantKeyExists = await this._tenantInfoService.CheckTenantKeyExisted(dto.Id, dto.TenantKey);
            if (isTenantKeyExists)
            {
                return Ok(false, $"{dto.TenantKey} 租户匹配规则已存在");
            }

            try
            {
                await _tenantInfoService.CreateOrUpdateTenantInfoAsync(dto);
                return Ok(true, $"{(dto.Id > 0 ? "编辑" : "新增")}租户成功");
            }
            catch (Exception ex)
            {
                return Ok(false, ex.Message);
            }

        }

        /// <summary>
        /// 初始化租户
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [CustomerResource("admin-add")]
        public async Task<IActionResult> OnPostInitializeAsync([FromBody] TenantInitializeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(false);
            }

            try
            {
                var tenantId = dto.TenantId;
                var tenantInfo = await _tenantInfoService.GetObjectAsync(z => z.Id == tenantId);
                if (tenantInfo == null)
                {
                    return Ok(false, "租户不存在");
                }

                //设置租户信息
                ISenparcEntitiesDbContext senparcDB = _adminUserInfoService.BaseData.BaseDB.BaseDataContext as ISenparcEntitiesDbContext;
                if (senparcDB == null)
                {
                    return Ok(false, "租户信息设置失败");
                }

                senparcDB.TenantInfo = new RequestTenantInfo()
                {
                    Id = tenantInfo.Id,
                    Name = tenantInfo.Name,
                    TenantKey = tenantInfo.TenantKey,
                };
                senparcDB.TenantInfo.TryMatch(true);

                var adminUserInfo = _adminUserInfoService.Init(dto.AdminAccount, out string password);

                await installerService.InitSystemAsync(dto.SystemName, adminUserInfo.Id, tenantInfo);


                return Ok(new
                {
                    tenantInfo = new
                    {
                        id = tenantInfo.Id,
                        name = tenantInfo.Name,
                        tenantKey = tenantInfo.TenantKey
                    },
                    adminAccount = new
                    {
                        username = dto.AdminAccount,
                        password = password
                    }
                }, true, "初始化成功");
            }
            catch (Exception ex)
            {
                return Ok(false, ex.Message);
            }
        }
    }

    public class TenantInitializeDto
    {
        public int TenantId { get; set; }
        public string SystemName { get; set; }
        public string AdminAccount { get; set; }
    }
}
