using Microsoft.AspNetCore.Http;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.Domain.Services
{
    public class InstallerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly XncfModuleServiceExtension _xncfModuleService;
        private readonly SysMenuService _sysMenuService;
        private readonly SystemConfigService _systemConfigService;
        private readonly TenantInfoService _tenantInfoService;
        private readonly AdminUserInfoService _accountInfoService;

        /// <summary>
        /// 新创建的 RequestTenantInfo
        /// </summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }
        public TenantRule TenantRule { get; set; }
        public bool MultiTenantEnable { get; set; }


        /// <summary>
        /// 初始化安装系统
        /// </summary>
        /// <returns></returns>
        private async Task InitSystemAsync()
        {
            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();

            Senparc.Xncf.SystemCore.Register systemCoreRegister = new Senparc.Xncf.SystemCore.Register();

            Senparc.Xncf.SystemManager.Register systemManagerRegister = new Senparc.Xncf.SystemManager.Register();

            Senparc.Xncf.SystemPermission.Register systemPermissionRegister = new Senparc.Xncf.SystemPermission.Register();

            Senparc.Xncf.XncfModuleManager.Register xncfModuleManagerRegister = new Senparc.Xncf.XncfModuleManager.Register();

            Senparc.Xncf.AreasBase.Register areasBaseRegister = new Senparc.Xncf.AreasBase.Register();

            Senparc.Xncf.Installer.Register installerRegister = new Senparc.Xncf.Installer.Register();

            Senparc.Xncf.Menu.Register menuRegister = new Senparc.Xncf.Menu.Register();

            {
                await InitDatabaseAsync(() => systemCoreRegister.InitDatabase(_serviceProvider));//TODO：目前实际已无效，不会构建任何数据库代码，此处仅作占位

                //await InitDatabaseAsync(() => systemManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => systemPermissionRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => xncfModuleManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => menuRegister.InitDatabase(_serviceProvider));


                //开始安装权限模块
                //（必须放在第一个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(systemPermissionRegister, installNow: false, addMenu: false);

                //开始安装菜单管理模块
                //（必须放在第二个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(menuRegister, installNow: false, addMenu: false);
                _sysMenuService.Init();

                //开始安装模块理管理模块
                //（必须放在第三个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(xncfModuleManagerRegister);

                //开始安装系统基础模块
                await InstallAndOpenModuleAsync(systemCoreRegister);

                //开始安装系统管理管理模块
                await InstallAndOpenModuleAsync(systemManagerRegister);

                //开始安装 Areas 模块
                await InstallAndOpenModuleAsync(areasBaseRegister);

                //开始安装 Installer 模块
                await InstallAndOpenModuleAsync(installerRegister);

                //安装租户模块
                if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                {
                    await InstallAndOpenModuleAsync(tenantRegister);
                }

                //将权限模块进行安装
                await InstallAndOpenModuleAsync(systemPermissionRegister, true, true);

                //将菜单模块进行安装
                await InstallAndOpenModuleAsync(menuRegister, true, true);

            }

            //TODO:选择性安装用户自定义模块

            {
                //开始安装并启用系统模块（Admin）
                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister);
                //确保已被赋值
                adminModule ??= await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);

                //一次性保存（所有）修改
                await _xncfModuleService.SaveObjectAsync(adminModule).ConfigureAwait(false);

                _systemConfigService.Init();//初始化系统信息
            }

            {
                //开始安装用户模块（如有）  TODO:选择性安装

            }

            //((SenparcEntities)_accountInfoService.BaseData.BaseDB.BaseDataContext).ResetMigrate();//重置合并状态
            //((SenparcEntities)_accountInfoService.BaseData.BaseDB.BaseDataContext).Migrate();//进行合并
        }

        private async Task InitDatabaseAsync(Func<Task<(bool, string)>> initDatabaseFunc)
        {
            //初始化数据库
            var (initDbSuccess, initDbMsg) = await initDatabaseFunc();

            Console.WriteLine($"完成 systemCoreRegister.InitDatabase，是否成功：{initDbSuccess}。数据库信息：{initDbMsg}");

            if (!initDbSuccess)
            {
                throw new NcfDatabaseException($"ServiceRegister.InitDatabase 失败：{initDbMsg}", DatabaseConfigurationFactory.Instance.Current.GetType());
            }
        }


        /// <summary>
        /// 安装并且开放模块
        /// </summary>
        /// <param name="register"></param>
        /// <param name="installNow">是否立即安装</param>
        /// <param name="addMenu">是否添加菜单（必须 installNow 为 true 时生效）</param>
        /// <returns></returns>
        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, bool installNow = true, bool addMenu = true)
        {
            //开始安装模块（创建数据库相关表）
            await register.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);

            XncfModule xncfModule = null;

            //安装模块
            if (installNow)
            {
                xncfModule = _xncfModuleService.GetObject(z => z.Uid == register.Uid);
                if (xncfModule == null)
                {
                    await _xncfModuleService.InstallModuleAsync(register.Uid, addMenu);
                    xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
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



        public InstallerService(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleService, SysMenuService sysMenuService, SystemConfigService systemConfigService, TenantInfoService tenantInfoService, AdminUserInfoService accountInfoService)
        {
            this._serviceProvider = serviceProvider;
            this._xncfModuleService = xncfModuleService;
            this._sysMenuService = sysMenuService;
            this._systemConfigService = systemConfigService;
            this._tenantInfoService = tenantInfoService;
            this._accountInfoService = accountInfoService;
        }

        /// <summary>
        /// 执行默认包的安装命令
        /// </summary>
        /// <returns></returns>
        public async Task<InstallDto> InstallAsync()
        {
            var installDto = new InstallDto()
            {
                StatCode = 404
            };

            //原 Get 请求
            {
                //添加初始化多租户信息
                if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                {
                    try
                    {
                        //初始化数据库

                        //var (initDbSuccess, initDbMsg) = await systemCoreRegister.InitDatabase(_serviceProvider/*, _tenantInfoService, *//*_httpContextAccessor.Value.HttpContext*/);

                        //安装多租户
                            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();
                            await tenantRegister.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
                    }
                    catch (Exception ex)
                    {
                        //如果已经安装过，则不处理
                        //TODO:特定的Exception
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                    finally
                    {

                    }
                }

                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister, installNow: false, addMenu: false);
            }

            //原Post后执行
            {
                var cacheStrategy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                using (var cacheLock = await cacheStrategy.BeginCacheLockAsync("InstallerService", "Install"))
                {
                    var adminUserInfo = _accountInfoService.Init(out string userName, out string password);//初始化管理员信息

                    if (adminUserInfo == null)
                    {
                        installDto.StatCode = 404;
                        return installDto;
                    }
                    else
                    {
                        installDto.Step = 1;

                        //进行系统初始化安装
                        await InitSystemAsync();

                        //IXncfRegister systemRegister = XncfRegisterManager.RegisterList.First(z => z.GetType() == typeof(Senparc.Areas.Admin.Register));
                        //await _xncfModuleService.InstallMenuAsync(systemRegister, Ncf.Core.Enums.InstallOrUpdate.Install);//安装菜单

                        installDto.AdminUserName = userName;
                        installDto.AdminPassword = password;//这里不可以使用 adminUserInfo.Password，因为此参数已经是加密信息
                        installDto.StatCode = 0;
                    }
                }
            }
            return installDto;
        }
    }
}
