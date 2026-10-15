/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml.cs implementation and project behavior.


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20261005
    修改描述：v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

----------------------------------------------------------------*/

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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<AdminResource> _localizer = serviceProvider.GetRequiredService<IStringLocalizer<AdminResource>>();
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

            var enableMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
            List<Dictionary<string, object>> rows = null;
            if (enableMultiTenant)
            {
                //多租户启用时，附带每个租户的管理员账号数量，便于删除/停用决策
                rows = tenantInfo.AsEnumerable().Select(z => new Dictionary<string, object>
                {
                    ["id"] = z.Id,
                    ["name"] = z.Name,
                    ["tenantKey"] = z.TenantKey,
                    ["adminRemark"] = z.AdminRemark,
                    ["enable"] = z.Enable,
                    ["addTime"] = z.AddTime,
                    ["lastUpdateTime"] = z.LastUpdateTime,
                    ["adminUserCount"] = 0
                }).ToList();

                foreach (var row in rows)
                {
                    row["adminUserCount"] = await _adminUserInfoService.GetTenantUserCountAsync((int)row["id"]);
                }
            }

            return Ok(new
            {
                List = rows ?? tenantInfo.AsEnumerable().Select(z => new Dictionary<string, object>
                {
                    ["id"] = z.Id,
                    ["name"] = z.Name,
                    ["tenantKey"] = z.TenantKey,
                    ["adminRemark"] = z.AdminRemark,
                    ["enable"] = z.Enable,
                    ["addTime"] = z.AddTime,
                    ["lastUpdateTime"] = z.LastUpdateTime,
                    ["adminUserCount"] = 0
                }),
                TotalCount = tenantInfo.TotalCount,
                PageIndex = tenantInfo.PageIndex,
                enableMultiTenant
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
                return Ok(false, _localizer["Tenant.NameExists", dto.Name]);
            }

            bool isTenantKeyExists = await this._tenantInfoService.CheckTenantKeyExisted(dto.Id, dto.TenantKey);
            if (isTenantKeyExists)
            {
                return Ok(false, _localizer["Tenant.KeyExists", dto.TenantKey]);
            }

            try
            {
                await _tenantInfoService.CreateOrUpdateTenantInfoAsync(dto);
                return Ok(true, dto.Id > 0 ? _localizer["Tenant.SaveSuccess.Edit"] : _localizer["Tenant.SaveSuccess.Create"]);
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
                    return Ok(false, _localizer["Tenant.NotFound"]);
                }

                //设置租户信息
                ISenparcEntitiesDbContext senparcDB = _adminUserInfoService.BaseData.BaseDB.BaseDataContext as ISenparcEntitiesDbContext;
                if (senparcDB == null)
                {
                    return Ok(false, _localizer["Tenant.SetInfoFailed"]);
                }

                senparcDB.TenantInfo = new RequestTenantInfo()
                {
                    Id = tenantInfo.Id,
                    Name = tenantInfo.Name,
                    TenantKey = tenantInfo.TenantKey,
                };
                senparcDB.TenantInfo.TryMatch(true);

                var adminUserInfoResult = await _adminUserInfoService.InitAsync(dto.AdminAccount);

                var adminUserInfo = adminUserInfoResult.AdminUserInfo;

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
                        password = adminUserInfoResult.Password
                    }
                }, true, _localizer["Tenant.InitializeSuccess"]);
            }
            catch (Exception ex)
            {
                return Ok(false, ex.Message);
            }
        }

        /// <summary>
        /// 删除租户（安全校验）：
        /// 1、不能删除当前正在使用的租户；
        /// 2、系统必须至少保留一个启用的租户；
        /// 3、租户下仍存在管理员账号时不允许删除（避免账号数据成为孤儿）。
        /// </summary>
        [CustomerResource("admin-delete")]
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return Ok(false);
            }

            try
            {
                var enableMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
                var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
                var currentTenantId = enableMultiTenant && requestTenantInfo.MatchSuccess ? requestTenantInfo.Id : 0;

                var allTenants = (await _tenantInfoService.GetFullListAsync(z => true)).ToList();
                var enabledCount = allTenants.Count(z => z.Enable);

                foreach (var id in ids)
                {
                    var tenantInfo = allTenants.FirstOrDefault(z => z.Id == id);
                    if (tenantInfo == null)
                    {
                        return Ok(false, _localizer["Tenant.DeleteNotFound", id]);
                    }

                    if (enableMultiTenant && currentTenantId > 0 && id == currentTenantId)
                    {
                        return Ok(false, _localizer["Tenant.DeleteCurrentTenantNotAllowed"]);
                    }

                    if (tenantInfo.Enable && enabledCount <= 1)
                    {
                        return Ok(false, _localizer["Tenant.DeleteLastTenantNotAllowed"]);
                    }

                    var adminUserCount = await _adminUserInfoService.GetTenantUserCountAsync(id);
                    if (adminUserCount > 0)
                    {
                        return Ok(false, _localizer["Tenant.DeleteHasAdminUsers", tenantInfo.Name, adminUserCount]);
                    }

                    await _tenantInfoService.DeleteObjectAsync(tenantInfo);
                    if (tenantInfo.Enable)
                    {
                        enabledCount--;
                    }
                    await tenantInfo.ClearCache(_serviceProvider);
                }

                return Ok(true, _localizer["Tenant.DeleteSuccess"]);
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
