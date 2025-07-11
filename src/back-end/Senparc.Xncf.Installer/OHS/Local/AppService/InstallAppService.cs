﻿using Microsoft.AspNetCore.Http;
using Senparc.Areas.Admin.Domain.Models.Dto;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.Installer.Domain;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.Domain.Services;
using Senparc.Xncf.Instraller.Pages;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.OHS.Local.AppService
{
    public class InstallAppService : AppServiceBase
    {
        private readonly InstallerService _installerService;
        private readonly TenantInfoService _tenantInfoService;
        private readonly Lazy<IHttpContextAccessor> _httpContextAccessor;

        public InstallAppService(IServiceProvider serviceProvider, InstallerService installerService, TenantInfoService tenantInfoService, Lazy<IHttpContextAccessor> httpContextAccessor) : base(serviceProvider)
        {
            this._installerService = installerService;
            this._tenantInfoService = tenantInfoService;
            this._httpContextAccessor = httpContextAccessor;
        }

        [ApiBind()]
        public async Task<AppResponseBase<InstallResponseDto>> InstallAsync(InstallRequestDto installRequestDto)
        {
            return await this.GetResponseAsync<AppResponseBase<InstallResponseDto>, InstallResponseDto>(async (response, logger) =>
            {
                return await _installerService.InstallAsync(installRequestDto, base.ServiceProvider);
            });
        }

        public async Task<AppResponseBase<GetDefaultInstallOptionsResponseDto>> GetInstallOptionsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetDefaultInstallOptionsResponseDto>, GetDefaultInstallOptionsResponseDto>(async (response, logger) =>
            {
                return await Task.FromResult(_installerService.GetDefaultInstallOptions());
            });
        }

        public async Task<AppResponseBase<TenantInfoDto>> GetTenantInfoAsync()
        {
            var httpContext = _httpContextAccessor.Value.HttpContext;
            var tenantInfo = await _tenantInfoService.CreateInitTenantInfoAsync(httpContext);

            //重置租户状态
            var createdRequestTenantInfo = await _tenantInfoService.SetScopedRequestTenantInfoAsync(httpContext);

            return await this.GetResponseAsync<AppResponseBase<TenantInfoDto>, TenantInfoDto>(async (response, logger) =>
            {
                return await Task.FromResult(tenantInfo);
            });
        }
    }
}