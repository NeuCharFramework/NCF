using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Service;
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

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Console.WriteLine("进入安装程序，检测是否需要初始化");

                MultipleDatabaseType = DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
                var adminUserInfo = await _accountInfoService.GetObjectAsync(z => true);//检查是否已初始化
                if (adminUserInfo == null)
                {
                    throw new Exception("需要初始化");
                }
            }
            catch (Exception)
            {
                {
                    Senparc.Service.Register serviceRegister = new Service.Register();

                    //添加初始化多租户信息
                    if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
                    {
                        var httpContext = _httpContextAccessor.Value.HttpContext;
                        try
                        {

                            //初始化数据库
                            var (initDbSuccess, initDbMsg) = await serviceRegister.InitDatabase(_serviceProvider, _tenantInfoService, _httpContextAccessor.Value.HttpContext);
                            if (!initDbSuccess)
                            {
                                throw new NcfDatabaseException($"ServiceRegister.InitDatabase 失败：{initDbMsg}", DatabaseConfigurationFactory.Instance.Current.GetType());
                            }

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

                    //开始安装系统模块（Service）
                    await serviceRegister.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
                    //启用系统模块（Service）
                    var serviceModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == serviceRegister.Uid);
                    serviceModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                }

                {
                    //开始安装系统模块（Admin）
                    Senparc.Areas.Admin.Register adminRegister = new Areas.Admin.Register();
                    await adminRegister.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Install);
                    //启用系统模块（Admin）
                    var adminModule = await _xncfModuleService.GetObjectAsync(z => z.Uid == adminRegister.Uid);
                    adminModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);

                    //一次性保存修改
                    await _xncfModuleService.SaveObjectAsync(adminModule).ConfigureAwait(false);
                }

                //((SenparcEntities)_accountInfoService.BaseData.BaseDB.BaseDataContext).ResetMigrate();//重置合并状态
                //((SenparcEntities)_accountInfoService.BaseData.BaseDB.BaseDataContext).Migrate();//进行合并
                return Page();
            }

            //base.Response.StatusCode = 404;
            return new StatusCodeResult(404);//已经安装完毕，且存在管理员则不进行安装
        }

        public async Task<IActionResult> OnPostAsync()
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

                _systemConfigService.Init();//初始化系统信息
                _sysMenuService.Init();

                IXncfRegister systemRegister = XncfRegisterManager.RegisterList.First(z => z.GetType() == typeof(Senparc.Areas.Admin.Register));
                await _xncfModuleService.InstallMenuAsync(systemRegister, Ncf.Core.Enums.InstallOrUpdate.Install);//安装菜单

                AdminUserName = userName;
                AdminPassword = password;//这里不可以使用 adminUserInfo.Password，因为此参数已经是加密信息

                return Page();
            }
        }
    }
}