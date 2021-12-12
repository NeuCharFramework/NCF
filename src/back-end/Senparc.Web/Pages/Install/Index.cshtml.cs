using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Web.Pages.Install
{
    public class IndexModel : PageModel //不使用基类，因为无法通过已安装程序自动检测
    {
        private readonly XncfModuleServiceExtension _xncfModuleService;
        private readonly AdminUserInfoService _accountInfoService;
        private readonly SystemConfigService _systemConfigService;
        private readonly SysMenuService _sysMenuService;
        private readonly TenantInfoService _tenantInfoService;
        private readonly Lazy<IHttpContextAccessor> _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 管理员用户名
        /// </summary>
        public string AdminUserName { get; set; }
        /// <summary>
        /// 管理员密码
        /// </summary>
        public string AdminPassword { get; set; }
        /// <summary>
        /// 需要修改的命名空间
        /// </summary>
        public string Namespace { get; set; }

        public int Step { get; set; }

        public MultipleDatabaseType MultipleDatabaseType { get; set; }

        /// <summary>
        /// 新创建的 RequestTenantInfo
        /// </summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }
        public TenantInfoDto TenantInfoDto { get; private set; }
        public TenantRule TenantRule { get; set; }

        public bool MultiTenantEnable { get; set; }


        public IndexModel(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleService, AdminUserInfoService accountService,
            SystemConfigService systemConfigService, SysMenuService sysMenuService, TenantInfoService tenantInfoService, Lazy<IHttpContextAccessor> httpContextAccessor)
        {
            _xncfModuleService = xncfModuleService;
            _accountInfoService = accountService;
            _sysMenuService = sysMenuService;
            _tenantInfoService = tenantInfoService;
            this._httpContextAccessor = httpContextAccessor;
            _systemConfigService = systemConfigService;
            _serviceProvider = serviceProvider;

            MultiTenantEnable = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
            TenantRule = SiteConfig.SenparcCoreSetting.TenantRule;
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

        private async Task<XncfModule> InstallAndOpenModuleAsync(IXncfRegister register, bool installNow = true, bool addMenu = true)
        {
            //开始安装模块
            await register.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);

            XncfModule xncfModule = null;

            //安装模块
            if (installNow)
            {
                xncfModule = _xncfModuleService.GetObject(z => z.Uid == register.Uid);
                if (xncfModule == null)
                {
                    await _xncfModuleService.InstallModuleAsync(register.Uid);
                    xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
                }

                //启用模块
                //xncfModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == register.Uid);
                xncfModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
            }

            if (addMenu)
            {
                //TODO:判断是否已存在
                await _xncfModuleService.InstallMenuAsync(register, Ncf.Core.Enums.InstallOrUpdate.Install);
            }

            return xncfModule;
        }

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

                //安装租户模块
                if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                {
                    await InstallAndOpenModuleAsync(tenantRegister);
                }


                //将权限模块进行安装
                await InstallAndOpenModuleAsync(systemPermissionRegister, true, true);

                //await _xncfModuleService.InstallModuleAsync(systemPermissionRegister.Uid);
                //await _xncfModuleService.InstallMenuAsync(systemPermissionRegister, Ncf.Core.Enums.InstallOrUpdate.Install);

                //将菜单模块进行安装
                await InstallAndOpenModuleAsync(menuRegister, true, true);
                //await _xncfModuleService.InstallModuleAsync(menuRegister.Uid);
                //await _xncfModuleService.InstallMenuAsync(menuRegister, Ncf.Core.Enums.InstallOrUpdate.Install);

            }

            //TODO:选择性安装用户自定义模块

            {
                //开始安装并启用系统模块（Admin）
                Senparc.Areas.Admin.Register adminRegister = new Areas.Admin.Register();
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

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Console.WriteLine("进入安装程序，检测是否需要初始化");

                MultipleDatabaseType = DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
                var adminUserInfo = await _accountInfoService.GetObjectAsync(z => true);//检查是否已初始化
                if (adminUserInfo == null)
                {
                    Console.WriteLine("需要初始化");
                    throw new Exception("需要初始化");
                }

                try
                {
                    if (Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                    {
                        //判断是不是从新的域名进入
                        RequestTenantInfo currentRequestTenantInfo = MultiTenantHelper.TryGetAndCheckRequestTenantInfo(_serviceProvider, null);
                    }
                }
                catch (Exception)
                {
                    throw;
                }

            }
            catch (Exception)
            {
                Console.WriteLine("开始初始化");

                {

                    //添加初始化多租户信息
                    if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                    {
                        var httpContext = _httpContextAccessor.Value.HttpContext;
                        try
                        {
                            //初始化数据库

                            //var (initDbSuccess, initDbMsg) = await systemCoreRegister.InitDatabase(_serviceProvider/*, _tenantInfoService, *//*_httpContextAccessor.Value.HttpContext*/);

                            //安装多租户
                            Senparc.Xncf.Tenant.Register tenantRegister = new Senparc.Xncf.Tenant.Register();
                            await tenantRegister.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);

                            var tenantInfo = await _tenantInfoService.CreateInitTenantInfoAsync(httpContext);

                            //重置租户状态
                            CreatedRequestTenantInfo = await _tenantInfoService.SetScopedRequestTenantInfoAsync(httpContext);
                            TenantInfoDto = _tenantInfoService.Mapper.Map<TenantInfoDto>(await _tenantInfoService.GetObjectAsync(z => z.Id == CreatedRequestTenantInfo.Id));
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

                    Senparc.Areas.Admin.Register adminRegister = new Areas.Admin.Register();
                    var adminModule = await InstallAndOpenModuleAsync(adminRegister, installNow: false, addMenu: false);
                }

                return Page();
            }

            //base.Response.StatusCode = 404;
            return new StatusCodeResult(404);//已经安装完毕，且存在管理员则不进行安装
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var cacheStrategy = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            using (var cacheLock = await cacheStrategy.BeginCacheLockAsync("Install", "OnPostAsync"))
            {
                var adminUserInfo = _accountInfoService.Init(out string userName, out string password);//初始化管理员信息

                if (adminUserInfo == null)
                {
                    return new StatusCodeResult(404);
                }
                else
                {
                    Step = 1;

                    //添加初始化多租户信息
                    if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                    {
                        var httpContext = _httpContextAccessor.Value.HttpContext;
                        try
                        {
                            //var tenantInfo = await _tenantInfoService.CreateInitTenantInfoAsync(httpContext);

                            CreatedRequestTenantInfo = await _tenantInfoService.SetScopedRequestTenantInfoAsync(httpContext);
                            TenantInfoDto = _tenantInfoService.Mapper.Map<TenantInfoDto>(await _tenantInfoService.GetObjectAsync(z => z.Id == CreatedRequestTenantInfo.Id));
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                        }
                    }

                    //进行系统初始化安装
                    await InitSystemAsync();

                    //IXncfRegister systemRegister = XncfRegisterManager.RegisterList.First(z => z.GetType() == typeof(Senparc.Areas.Admin.Register));
                    //await _xncfModuleService.InstallMenuAsync(systemRegister, Ncf.Core.Enums.InstallOrUpdate.Install);//安装菜单

                    AdminUserName = userName;
                    AdminPassword = password;//这里不可以使用 adminUserInfo.Password，因为此参数已经是加密信息

                    return Page();
                }
            }
        }
    }
}