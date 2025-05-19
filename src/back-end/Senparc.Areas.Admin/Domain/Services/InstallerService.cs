using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NcfSysMenuService = Senparc.Ncf.Service.SysMenuService;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class InstallerService(IServiceProvider serviceProvider, TenantInfoService tenantInfoService, XncfModuleServiceExtension xncfModuleServiceExtension)
    {
        public async Task InitSystemAsync(string systemName,int adminUserInfoId, TenantInfo tenantInfo)
        {
            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();

            Senparc.Xncf.SystemCore.Register systemCoreRegister = new Senparc.Xncf.SystemCore.Register();

            Senparc.Xncf.SystemManager.Register systemManagerRegister = new Senparc.Xncf.SystemManager.Register();

            Senparc.Xncf.SystemPermission.Register systemPermissionRegister = new Senparc.Xncf.SystemPermission.Register();

            Senparc.Xncf.XncfModuleManager.Register xncfModuleManagerRegister = new Senparc.Xncf.XncfModuleManager.Register();

            Senparc.Xncf.AreasBase.Register areasBaseRegister = new Senparc.Xncf.AreasBase.Register();

            //Senparc.Xncf.Installer.Register installerRegister = new Senparc.Xncf.Installer.Register();

            Senparc.Xncf.Menu.Register menuRegister = new Senparc.Xncf.Menu.Register();

            {
                //开始安装权限模块
                //（必须放在第一个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, tenantInfo, installNow: false, addMenu: false);

                //开始安装菜单管理模块
                //（必须放在第二个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, tenantInfo, installNow: false, addMenu: false);

                try
                {
                    //安装权限、菜单等默认模块和配置

                    NcfSysMenuService _sysMenuService = serviceProvider.GetService<NcfSysMenuService>();
                    //.Init 内部还会执行一次
                    _sysMenuService.SetTenantInfoForAllServices(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                    _sysMenuService.Init(tenantInfoService.GetRequestTenantInfo(tenantInfo), adminUserInfoId);
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("_sysMenuService.Init 异常", ex.Message);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }

                //开始安装模块理管理模块
                //（必须放在第三个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(xncfModuleManagerRegister, serviceProvider, tenantInfo);

                //开始安装系统基础模块
                await InstallAndOpenModuleAsync(systemCoreRegister, serviceProvider, tenantInfo);

                //开始安装系统管理管理模块
                await InstallAndOpenModuleAsync(systemManagerRegister, serviceProvider, tenantInfo);

                //开始安装 Areas 模块
                await InstallAndOpenModuleAsync(areasBaseRegister, serviceProvider, tenantInfo);

                ////开始安装 Installer 模块
                //await InstallAndOpenModuleAsync(installerRegister, serviceProvider);

                //安装租户模块
                //if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                //{
                //    await InstallAndOpenModuleAsync(tenantRegister, serviceProvider, tenantInfo);
                //}

                //将权限模块进行安装
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, tenantInfo, true, true);

                //将菜单模块进行安装
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, tenantInfo, true, true);
            }

            //TODO:选择性安装用户自定义模块
            {
                List<string> installedList = new List<string> { "00000000-0000-0000-0001-000000000001", "00000000-0000-0000-0001-000000000002", "00000000-0000-0000-0001-000000000003", "00000000-0000-0000-0001-000000000004"
                , "00000000-0000-0000-0001-000000000005", "00000000-0000-0000-0001-000000000006","00000000-0000-0001-0001-000000000001", "62FBB022-B04E-423F-82FE-926D418A0815"};
                var _xncfModuleService = xncfModuleServiceExtension;// serviceProvider.GetService<XncfModuleServiceExtension>();
                //if (needModelList != null)
                //{
                //    foreach (var needModelId in needModelList)
                //    {
                //        if (!installedList.Contains(needModelId))
                //        {
                //            //var docRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == needModel);
                //            var docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                //            if (docModule == null)
                //            {
                //                await _xncfModuleService.InstallModuleAsync(needModelId);
                //                docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                //            }
                //            //开启模块
                //            if (docModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                //            {
                //                docModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                //                await _xncfModuleService.SaveObjectAsync(docModule);
                //            }
                //        }
                //    }
                //}
            }


            {
                var _xncfModuleService = xncfModuleServiceExtension;
                _xncfModuleService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                //开始安装并启用系统模块（Admin）
                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister, serviceProvider, tenantInfo);
                //确保已被赋值
                adminModule ??= await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);

                //一次性保存（所有）修改
                await _xncfModuleService.SaveObjectAsync(adminModule).ConfigureAwait(false);

                var _systemConfigService = serviceProvider.GetService<SystemConfigService>();
                _systemConfigService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));
                _systemConfigService.Init(systemName);//初始化系统信息
            }

            {
                //开始安装用户模块（如有）  TODO:选择性安装

            }
        }


        /// <summary>
        /// 安装并且开放模块
        /// </summary>
        /// <param name="register"></param>
        /// <param name="installNow">是否立即安装</param>
        /// <param name="addMenu">是否添加菜单（必须 installNow 为 true 时生效）</param>
        /// <returns></returns>
        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, IServiceProvider serviceProvider, TenantInfo tenantInfo, bool installNow = true, bool addMenu = true)
        {
            try
            {
                //开始安装模块（创建数据库相关表）
                await register.InstallOrUpdateAsync(serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);

                throw;
            }


            XncfModule xncfModule = null;

            //安装模块
            if (installNow)
            {
                var _xncfModuleService = xncfModuleServiceExtension;// serviceProvider.GetService<XncfModuleServiceExtension>();

                //设置租户信息
                _xncfModuleService.SetTenantInfo(tenantInfoService.GetRequestTenantInfo(tenantInfo));

                xncfModule = _xncfModuleService.GetObject(z => z.Uid == register.Uid);
                if (xncfModule == null)
                {
                    try
                    {
                        await _xncfModuleService.InstallModuleAsync(register.Uid, addMenu);
                        xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("_xncfModuleService.InstallModuleAsync 异常：", ex.Message);
                        SenparcTrace.BaseExceptionLog(ex);
                        throw;
                    }
                }

                //启用模块
                //xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
                xncfModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
            }

            //if (addMenu)
            //{
            //    //TODO:判断是否已存在
            //    await _xncfModuleService.InstallMenuAsync(register, Ncf.Core.Enums.InstallOrUpdate.Install);
            //}

            return xncfModule;
        }
    }
}
