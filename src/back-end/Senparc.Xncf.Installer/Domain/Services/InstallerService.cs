using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.Tenant.Domain.Services;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.Domain.Services
{
    public class InstallerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly InstallOptionsService _installOptionsService;

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
        private async Task InitSystemAsync(string systemName, IServiceProvider serviceProvider, List<string> needModelList)
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
                await InitDatabaseAsync(() => systemCoreRegister.InitDatabase(serviceProvider));//TODO：目前实际已无效，不会构建任何数据库代码，此处仅作占位

                //await InitDatabaseAsync(() => systemManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => systemPermissionRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => xncfModuleManagerRegister.InitDatabase(_serviceProvider));
                //await InitDatabaseAsync(() => menuRegister.InitDatabase(_serviceProvider));


                //开始安装权限模块
                //（必须放在第一个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, installNow: false, addMenu: false);

                //开始安装菜单管理模块
                //（必须放在第二个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, installNow: false, addMenu: false);

                try
                {
                    SysMenuService _sysMenuService = serviceProvider.GetService<SysMenuService>();

                    _sysMenuService.Init();
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("_sysMenuService.Init 异常", ex.Message);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }

                //开始安装模块理管理模块
                //（必须放在第三个，其他模块操作都需要依赖此模块）
                await InstallAndOpenModuleAsync(xncfModuleManagerRegister, serviceProvider);

                //开始安装系统基础模块
                await InstallAndOpenModuleAsync(systemCoreRegister, serviceProvider);

                //开始安装系统管理管理模块
                await InstallAndOpenModuleAsync(systemManagerRegister, serviceProvider);

                //开始安装 Areas 模块
                await InstallAndOpenModuleAsync(areasBaseRegister, serviceProvider);

                //开始安装 Installer 模块
                await InstallAndOpenModuleAsync(installerRegister, serviceProvider);

                //安装租户模块
                if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                {
                    await InstallAndOpenModuleAsync(tenantRegister, serviceProvider);
                }

                //将权限模块进行安装
                await InstallAndOpenModuleAsync(systemPermissionRegister, serviceProvider, true, true);

                //将菜单模块进行安装
                await InstallAndOpenModuleAsync(menuRegister, serviceProvider, true, true);
            }

            //TODO:选择性安装用户自定义模块
            {
                List<string> installedList = new List<string> { "00000000-0000-0000-0001-000000000001", "00000000-0000-0000-0001-000000000002", "00000000-0000-0000-0001-000000000003", "00000000-0000-0000-0001-000000000004"
                , "00000000-0000-0000-0001-000000000005", "00000000-0000-0000-0001-000000000006","00000000-0000-0001-0001-000000000001", "62FBB022-B04E-423F-82FE-926D418A0815"};
                var _xncfModuleService = serviceProvider.GetService<XncfModuleServiceExtension>();
                foreach (var needModelId in needModelList)
                {
                    if (!installedList.Contains(needModelId))
                    {
                        //var docRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == needModel);
                        var docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                        if (docModule == null)
                        {
                            await _xncfModuleService.InstallModuleAsync(needModelId);
                            docModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == needModelId);
                        }
                        //开启模块
                        if (docModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                        {
                            docModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                            await _xncfModuleService.SaveObjectAsync(docModule);
                        }
                    }
                }
            }


            {
                var _xncfModuleService = serviceProvider.GetService<XncfModuleServiceExtension>();

                //开始安装并启用系统模块（Admin）
                Senparc.Areas.Admin.Register adminRegister = new Senparc.Areas.Admin.Register();
                var adminModule = await InstallAndOpenModuleAsync(adminRegister, serviceProvider);
                //确保已被赋值
                adminModule ??= await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);

                //一次性保存（所有）修改
                await _xncfModuleService.SaveObjectAsync(adminModule).ConfigureAwait(false);

                var _systemConfigService = serviceProvider.GetService<SystemConfigService>();
                _systemConfigService.Init(systemName);//初始化系统信息
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
        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, IServiceProvider serviceProvider, bool installNow = true, bool addMenu = true)
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
                var _xncfModuleService = serviceProvider.GetService<XncfModuleServiceExtension>();

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

        /// <summary>
        /// 检验安装请求内各项配置的值是否合法
        /// </summary>
        /// <param name="installRequestDto">安装请求</param>
        /// <returns></returns>
        private bool VerifyInstallRequest(InstallRequestDto installRequestDto)
        {
            if (installRequestDto.DbConnectionString.IsNullOrEmpty())
            {
                return false;
            }

            if (installRequestDto.SystemName.IsNullOrEmpty())
            {
                return false;
            }

            if (installRequestDto.AdminUserName.IsNullOrEmpty())
            {
                return false;
            }
            return true;
        }


        public InstallerService(IServiceProvider serviceProvider, InstallOptionsService installOptionsService)
        {
            this._serviceProvider = serviceProvider;
            this._installOptionsService = installOptionsService;
        }

        public GetDefaultInstallOptionsResponseDto GetDefaultInstallOptions()
        {
            var result = new GetDefaultInstallOptionsResponseDto();

            //读取现有配置的默认值
            result.DbConnectionString = _installOptionsService.GetDbConnectionString();
            result.SystemName = _installOptionsService.GetDefaultSystemName();
            result.AdminUserName = _installOptionsService.GetDefaultAdminUserName();
            result.NeedModelList = _installOptionsService.GetModules();

            return result;
        }

        /// <summary>
        /// 执行默认包的安装命令
        /// </summary>
        /// <returns></returns>
        public async Task<InstallResponseDto> InstallAsync(InstallRequestDto installRequestDto, IServiceProvider serviceProvider)
        {
            using (var sope = serviceProvider.CreateScope())
            {
                var installResponseDto = new InstallResponseDto();
                installResponseDto.StatCode = 404;

                if (VerifyInstallRequest(installRequestDto) == false)
                {
                    return installResponseDto;
                }

                //比对传入的和原有的数据库连接字符串
                if (installRequestDto.DbConnectionString != _installOptionsService.GetDbConnectionString())
                {
                    _installOptionsService.ResetDbConnectionString(installRequestDto.DbConnectionString);
                }

                #region 请勿使用 EnsureCreatedAsync() 方法，系统将自动通过 database update 过程创建数据库
                //重新建立 Service 
                //var adminService = sope.ServiceProvider.GetService<AdminUserInfoService>();

                //var database = adminService.BaseClientRepository.BaseDB.BaseDataContext.Database;
                //var created = await database.EnsureCreatedAsync();//尝试创建数据库

                //await Console.Out.WriteLineAsync("尝试创建数据库：" + (created ? "成功创建" : "已存在，无需创建"));
                #endregion

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
                            await tenantRegister.InstallOrUpdateAsync(sope.ServiceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
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
                    var adminModule = await InstallAndOpenModuleAsync(adminRegister, sope.ServiceProvider, installNow: false, addMenu: false);
                }

                //原Post后执行
                {
                    var cacheStrategy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
                    using (var cacheLock = await cacheStrategy.BeginCacheLockAsync("InstallerService", "Install"))
                    {
                        var _accountInfoService = sope.ServiceProvider.GetService<AdminUserInfoService>();

                        var adminUserInfo = _accountInfoService.Init(installRequestDto.AdminUserName, out string password);//初始化管理员信息

                        if (adminUserInfo == null)
                        {
                            installResponseDto.StatCode = 404;
                            return installResponseDto;
                        }
                        else
                        {
                            installResponseDto.Step = 1;

                            //进行系统初始化安装
                            await InitSystemAsync(installRequestDto.SystemName, sope.ServiceProvider, installRequestDto.NeedModelList);

                            //IXncfRegister systemRegister = XncfRegisterManager.RegisterList.First(z => z.GetType() == typeof(Senparc.Areas.Admin.Register));
                            //await _xncfModuleService.InstallMenuAsync(systemRegister, Ncf.Core.Enums.InstallOrUpdate.Install);//安装菜单

                            installResponseDto.AdminUserName = installRequestDto.AdminUserName;
                            installResponseDto.AdminPassword = password;//这里不可以使用 adminUserInfo.Password，因为此参数已经是加密信息
                            installResponseDto.StatCode = 0;
                        }
                    }
                }
                return installResponseDto;
            }
        }
    }
}
