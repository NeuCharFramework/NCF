using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    [BackendJwtAuthorize]
    public class ModuleAppService : LocalAppServiceBase
    {
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx;
        public ModuleAppService(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleServiceEx) : base(serviceProvider)
        {
            _xncfModuleServiceEx = xncfModuleServiceEx;
        }

        /// <summary>
        /// 获取模块统计状态
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<Module_StatResponse>> StatAsync()
        {
            var response = await this.GetResponseAsync<AppResponseBase<Module_StatResponse>, Module_StatResponse>(async (response, logger) =>
            {
                //所有已安装模块
                var installedXncfModules = await _xncfModuleServiceEx.GetFullListAsync(z => true);
                //未安装或待升级模块
                var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);
                //未安装货代升级模块的版本
                var newVersions = updateXncfRegisters.Select(z => _xncfModuleServiceEx.GetVersionDisplayName(installedXncfModules, z));
                //需要升级的版本号
                var newXncfCount = newVersions.Count(z => !z.Contains("->"));
                //全新未安装的版本号
                var updateVersionXncfCount = newVersions.Count() - newXncfCount;
                //安装后缺失的模块
                var xncfRegisterManager = new XncfRegisterManager(base.ServiceProvider);
                var missingXncfCount = installedXncfModules.Count(z => !XncfRegisterManager.RegisterList.Exists(r => r.Uid == z.Uid));

                var data = new Module_StatResponse()
                {
                    InstalledXncfCount = installedXncfModules.Count,
                    UpdateVersionXncfCount = updateVersionXncfCount,
                    NewXncfCount = newXncfCount,
                    MissingXncfCount = missingXncfCount
                };

                return data;
            });
            return response;
        }
    }
}
