/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260729
    修改描述：v0.4.1 加强安装状态校验并收紧安装辅助路由

    修改标识：Senparc - 20260804
    修改描述：v0.5.0 适配数据库升级维护流程

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.AgentKernel;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.Domain.Services;
using Senparc.Xncf.Installer.OHS.Local.AppService;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.OHS.Remote;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Senparc.Xncf.Installer;

namespace Senparc.Xncf.Instraller.Pages
{
    [AutoValidateAntiforgeryToken]
    public class IndexModel : PageModel //不使用基类，因为无法通过已安装程序自动检测
    {
        private readonly AdminUserInfoService _accountInfoService;
        private readonly InstallAppService _installAppService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IStringLocalizer<InstallerResource> _localizer;

        /// <summary>
        /// 系统名称
        /// </summary>
        public string SystemName { get; set; }
        /// <summary>
        /// 管理员用户名
        /// </summary>
        public string AdminUserName { get; set; }
        /// <summary>
        /// 管理员密码
        /// </summary>
        public string AdminPassword { get; set; }
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DbConnectionString { get; set; }
        /// <summary>
        /// 需要修改的命名空间
        /// </summary>
        public string Namespace { get; set; }
        public int Step { get; set; }

        /// <summary>
        /// 需要安装的模块
        /// </summary>
        public List<XncfRegisterDto> NeedModelList { get; set; }


        public MultipleDatabaseType MultipleDatabaseType { get; set; }

        /// <summary>
        /// 新创建的 RequestTenantInfo
        /// </summary>
        public RequestTenantInfo CreatedRequestTenantInfo { get; set; }

        public bool InstallingTenant { get; private set; }
        public TenantInfoDto TenantInfoDto { get; private set; }
        public TenantRule TenantRule { get; set; }
        public bool MultiTenantEnable { get; set; }

        public IndexModel(IServiceProvider serviceProvider,
            AdminUserInfoService accountService,
            XncfModuleServiceExtension xncfModuleServiceEx,
            InstallAppService installAppService,
            IStringLocalizer<InstallerResource> localizer)
        {
            _serviceProvider = serviceProvider;
            _accountInfoService = accountService;
            this._installAppService = installAppService;
            _localizer = localizer;

            MultiTenantEnable = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
            TenantRule = SiteConfig.SenparcCoreSetting.TenantRule;
        }

        public async Task<IActionResult> OnGetAsync(string forceUpdateModule,int tenantId=0)
        {
            Console.WriteLine("进入安装程序，检测是否需要初始化");

            if (Request.IsLocal() && !forceUpdateModule.IsNullOrEmpty())
            {
                // 强制升级仍仅允许本机请求。
                Console.WriteLine("强制升级模块：" + forceUpdateModule);

                SenparcTrace.SendCustomLog("强制更新模块", $"开始：{forceUpdateModule}");
                var register = Senparc.CO2NET.Helpers.ReflectionHelper.CreateInstance<IXncfRegister>(forceUpdateModule + ".Register", forceUpdateModule);
                await register.InstallOrUpdateAsync(_serviceProvider, Ncf.Core.Enums.InstallOrUpdate.Update);
                SenparcTrace.SendCustomLog("强制更新模块", $"完成：{forceUpdateModule}");

                return Content(_localizer["Install.ForceUpgrade.Completed", forceUpdateModule]);
            }

            var installFinished = SiteConfig.CheckInstallFinishedFileExisted();

            try
            {
                MultipleDatabaseType = DatabaseConfigurationFactory.Instance.Current.MultipleDatabaseType;
                var adminUserInfo = await _accountInfoService.GetObjectAsync(z => true);//检查是否已初始化
                if (adminUserInfo != null)
                {
                    // A completed database can outlive the local marker file
                    // (for example after a deployment or a copied App_Data
                    // directory). Reconcile that state before deciding that the
                    // installer should be hidden.
                    if (!installFinished && IsSystemInitialized())
                    {
                        SiteConfig.IsInstalling = false;
                        SiteConfig.SetInstallFinished();
                        return new RedirectResult("/");
                    }

                    SenparcTrace.SendCustomLog("风险提示", "Install 被访问，已返回 404 进行混淆。如果您已经确保完成项目初始化，建议移除 Senparc.Xncf.Install 模块");
                    return new StatusCodeResult(404);
                }

                SiteConfig.IsInstalling = true;
                Console.WriteLine("需要初始化，开始加载安装选项");
                var result = await _installAppService.GetInstallOptionsAsync();
                SystemName = result.Data.SystemName;
                AdminUserName = result.Data.AdminUserName;
                DbConnectionString = result.Data.DbConnectionString;
                NeedModelList = result.Data.NeedModelList;
                return Page();
            }
            catch (Exception ex) when (DatabaseInstallState.IsSchemaUpgradeRequired(ex))
            {
                // 已有数据库缺少新字段属于升级状态，绝不能重新开放首次安装流程。
                return new RedirectResult("/Maintenance/DatabaseUpgrade");
            }
            catch (Exception ex) when (InstallDatabaseState.IsDatabaseUnavailableForInstallation(ex))
            {
                // Preserve the original installation entry behavior: an
                // unavailable or not-yet-created database is an expected state
                // while the installer is being opened. The POST operation still
                // has to create/update the database successfully.
                SiteConfig.IsInstalling = true;
                Console.WriteLine("开始初始化");

                var result = await _installAppService.GetInstallOptionsAsync();
                SystemName = result.Data.SystemName;
                AdminUserName = result.Data.AdminUserName;
                DbConnectionString = result.Data.DbConnectionString;
                NeedModelList = result.Data.NeedModelList;
                return Page();
            }
            catch (Exception ex)
            {
                SiteConfig.IsInstalling = false;
                SenparcTrace.BaseExceptionLog(ex);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        public async Task<IActionResult> OnPostAsync([FromBody] InstallRequestDto installRequestDto)
        {
            if (!SiteConfig.IsInstalling || installRequestDto == null)
            {
                return new StatusCodeResult(404);
            }

            try
            {
                if (await _accountInfoService.GetObjectAsync(z => true) != null)
                {
                    SiteConfig.IsInstalling = false;
                    return new StatusCodeResult(404);
                }
            }
            catch (Exception ex) when (InstallDatabaseState.IsDatabaseUnavailableForInstallation(ex))
            {
                // InstallerService creates/updates the schema before its guarded
                // re-check. A missing schema or unavailable SQL Server target
                // therefore remains a valid first-install request.
            }
            catch (Exception ex)
            {
                SiteConfig.IsInstalling = false;
                SenparcTrace.BaseExceptionLog(ex);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            //开始安装
            var result = await _installAppService.InstallAsync(installRequestDto);
            if (result.Success != true)
            {
                if (result.Data == null)
                {
                    return new StatusCodeResult(505);
                }
                return new StatusCodeResult(result.Data.StatCode);
            }

            AdminUserName = result.Data.AdminUserName;
            AdminPassword = result.Data.AdminPassword;
            Step = result.Data.Step;

            MultiTenantEnable = SiteConfig.SenparcCoreSetting.EnableMultiTenant;

            //添加初始化多租户信息
            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                var tenantReulst = await _installAppService.GetTenantInfoAsync();
                TenantInfoDto = tenantReulst.Data;
            }

            //撤销安装状态
            SiteConfig.IsInstalling = false;
            SiteConfig.SetInstallFinished();
            TenantMiddleware.FirstRunAndInstalling = false;

            return Page();
        }

        public IActionResult OnGetDefaultOptions()
        {
            return new JsonResult(_installAppService.GetInstallOptionsAsync());
        }

        private bool IsSystemInitialized()
        {
            try
            {
                return _serviceProvider.GetService<FullSystemConfigCache>()?.Data != null;
            }
            catch (NcfUninstallException)
            {
                return false;
            }
        }
    }
}
