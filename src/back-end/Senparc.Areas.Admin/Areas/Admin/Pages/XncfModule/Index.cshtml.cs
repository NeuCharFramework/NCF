/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：后台管理页面处理逻辑
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强后台模块批量更新并完善多语言管理界面

    修改标识：Senparc - 20260729
    修改描述：v0.2.0 增强后台管理员交互与桌面 Admin Chat 安全同步

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.SystemManager.Domain.Service;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Ncf.AreaBase.Admin.Filters;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AdminAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
    public class XncfModuleIndexModel : BaseAdminPageModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx;
        private readonly SysMenuService _sysMenuService;
        private readonly IStringLocalizer<AdminResource> _localizer;

        //TODO:从其他模块获得，或独立到对应模块的API
        private readonly Lazy<SystemConfigService> _systemConfigService;

        public XncfModuleIndexModel(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleServiceEx,
            SysMenuService sysMenuService, Lazy<SystemConfigService> systemConfigService,
            IStringLocalizer<AdminResource> localizer)
            : base(serviceProvider)
        {
            CurrentMenu = "XncfModule";

            this._serviceProvider = serviceProvider;
            this._xncfModuleServiceEx = xncfModuleServiceEx;
            this._sysMenuService = sysMenuService;
            this._systemConfigService = systemConfigService;
            this._localizer = localizer;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 数据库已存的XncfModules
        /// </summary>
        public PagedList<XncfModule> XncfModules { get; set; }
        public List<IXncfRegister> NewXncfRegisters { get; set; }

        private void LoadNewXncfRegisters(PagedList<XncfModule> xncfModules)
        {
            NewXncfRegisters = XncfRegisterManager.RegisterList.Where(z => !z.IgnoreInstall && !xncfModules.Exists(m => m.Uid == z.Uid && m.Version == z.Version)).ToList() ?? new List<IXncfRegister>();
        }

        public async Task OnGetAsync()
        {
            //更新菜单缓存
            await _sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
            XncfModules = await _xncfModuleServiceEx.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            LoadNewXncfRegisters(XncfModules);
        }

        /// <summary>
        /// 扫描新模块
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetScanAsync(string uid)
        {
            var result = await _xncfModuleServiceEx.InstallModuleAsync(uid, true);
            XncfModules = result.Item1;
            base.SetMessager(Ncf.Core.Enums.MessageType.info, result.Item2, true);

            //if (backpage=="Start")
            return RedirectToPage("Start", new { uid = uid });//始终到详情页
            //return RedirectToPage("Index");
        }

        /// <summary>
        /// 隐藏“模块管理”功能
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostHideManagerAsync()
        {
            //TODO:使用DTO操作
            var systemConfig = _systemConfigService.Value.GetObject(z => true);
            systemConfig.Update(systemConfig.SystemName, systemConfig.MchId, systemConfig.MchKey, systemConfig.TenPayAppId,
                systemConfig.HideModuleManager.HasValue && systemConfig.HideModuleManager.Value == true ? false : true);
            await _systemConfigService.Value.SaveObjectAsync(systemConfig);
            if (systemConfig.HideModuleManager == true)
            {
                return RedirectToPage("../Index");
            }
            else
            {
                return RedirectToPage("./Index");
            }
        }

        /// <summary>
        /// 隐藏“模块管理”功能 handler=HideManagerAjax
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostHideManagerAjaxAsync()
        {
            //TODO:使用DTO操作
            var systemConfig = _systemConfigService.Value.GetObject(z => true);
            systemConfig.Update(systemConfig.SystemName, systemConfig.MchId, systemConfig.MchKey, systemConfig.TenPayAppId,
                            systemConfig.HideModuleManager.HasValue && systemConfig.HideModuleManager.Value == true ? false : true); await _systemConfigService.Value.SaveObjectAsync(systemConfig);
            //if (systemConfig.HideModuleManager == true)
            //{
            //    return RedirectToPage("../Index");
            //}
            //else
            //{
            //    return RedirectToPage("./Index");
            //}
            return Ok(new { systemConfig.HideModuleManager });
        }

        /// <summary>
        /// 获取已安装模块 handler=Modules
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMofulesAsync(int pageIndex = 0, int pageSize = 0)
        {
            //更新菜单缓存
            await _sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
            PagedList<XncfModule> xncfModules = await _xncfModuleServiceEx.GetObjectListAsync(pageIndex, pageSize, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            //xncfModules.FirstOrDefault().
            var xncfRegisterList = XncfRegisterList.Select(_ => new { _.Uid, homeUrl = _.GetAreaHomeUrl(), _.Icon });
            var result = from xncfModule in xncfModules
                         join xncfRegister in xncfRegisterList on xncfModule.Uid equals xncfRegister.Uid
                         into xncfRegister_left
                         from xncfRegister in xncfRegister_left.DefaultIfEmpty()
                         select new
                         {
                             xncfModule,
                             xncfRegister
                         };
            return Ok(new { result, FullSystemConfig.HideModuleManager });
        }

        /// <summary>
        /// 获取未安装模块 handler=UnModules
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetUnMofulesAsync()
        {
            //所有已安装的模块
            var oldXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, z => true, z => z.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            //未安装或版本已更新（不同）的模块
            //var newXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(oldXncfModules);
            var newXncfRegisters = _xncfModuleServiceEx.GetOnlyUnInstallXncfModule(oldXncfModules);

            return Ok(newXncfRegisters.Select(z => new
            {
                z.MenuName,
                z.Name,
                z.Uid,
                Version = _xncfModuleServiceEx.GetVersionDisplayName(oldXncfModules, z),
                z.Icon
            })); ;
        }

        /// <summary>
        /// 获取待更新模块 handler=UpdatedModules
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetUpdatedMofulesAsync()
        {
            //所有已安装的模块
            var oldXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, z => true, z => z.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            //未安装或版本已更新（不同）的模块
            var newXncfRegisters = _xncfModuleServiceEx.GetUpdatedInstallXncfModule(oldXncfModules);

            return Ok(newXncfRegisters.Select(z => new
            {
                z.MenuName,
                z.Name,
                z.Uid,
                Version = _xncfModuleServiceEx.GetVersionDisplayName(oldXncfModules, z),
                z.Icon
            })); ;
        }

        /// <summary>
        /// 扫描新模块 handler=ScanAjax
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetScanAjaxAsync(string uid)
        {
            var result = await _xncfModuleServiceEx.InstallModuleAsync(uid, true);
            //XncfModules = result.Item1;
            //base.SetMessager(Ncf.Core.Enums.MessageType.info, result.Item2, true);
            return Ok(result.XncfModuleList);
            //return RedirectToPage("Index");
        }

        /// <summary>
        /// 批量更新并启用所选模块。模块安装/迁移按顺序执行，避免并发修改菜单和数据库结构。
        /// handler=BatchUpdateAndEnable
        /// </summary>
        public async Task<IActionResult> OnPostBatchUpdateAndEnableAsync([FromBody] BatchUpdateAndEnableXncfModulesRequest request)
        {
            var uids = request?.Uids?
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Select(z => z.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (uids.Count == 0)
            {
                return Ok(CreateBatchResult(
                    new List<BatchUpdateAndEnableXncfModuleResult>(),
                    _localizer["Xncf.BatchUpdate.SelectAtLeastOne"]));
            }

            var results = new List<BatchUpdateAndEnableXncfModuleResult>();
            foreach (var uid in uids)
            {
                var register = XncfRegisterManager.RegisterList.FirstOrDefault(z =>
                    string.Equals(z.Uid, uid, StringComparison.OrdinalIgnoreCase) && !z.IgnoreInstall);
                var item = new BatchUpdateAndEnableXncfModuleResult
                {
                    Uid = uid,
                    ModuleName = register?.MenuName ?? uid,
                    TargetVersion = register?.Version
                };

                try
                {
                    if (register == null)
                    {
                        throw new InvalidOperationException(_localizer["Xncf.BatchUpdate.ModuleUnavailable"]);
                    }

                    var installedModule = await _xncfModuleServiceEx
                        .GetObjectAsync(z => z.Uid == register.Uid)
                        .ConfigureAwait(false);
                    if (installedModule == null)
                    {
                        throw new InvalidOperationException(_localizer["Xncf.ModuleNotInstalled"]);
                    }

                    item.PreviousVersion = installedModule.Version;
                    if (!string.Equals(installedModule.Version, register.Version, StringComparison.OrdinalIgnoreCase))
                    {
                        var updateResult = await _xncfModuleServiceEx
                            .InstallModuleAsync(register.Uid, true)
                            .ConfigureAwait(false);
                        if (updateResult.InstallOrUpdate != Ncf.Core.Enums.InstallOrUpdate.Update)
                        {
                            throw new InvalidOperationException(_localizer["Xncf.BatchUpdate.UpdateNotApplied"]);
                        }
                    }

                    item.UpdateSucceeded = true;
                    var updatedModule = await _xncfModuleServiceEx
                        .GetObjectAsync(z => z.Uid == register.Uid)
                        .ConfigureAwait(false);
                    if (updatedModule == null)
                    {
                        throw new InvalidOperationException(_localizer["Xncf.ModuleNotInstalled"]);
                    }

                    item.FinalVersion = updatedModule.Version;
                    if (!string.Equals(updatedModule.Version, register.Version, StringComparison.OrdinalIgnoreCase))
                    {
                        item.UpdateSucceeded = false;
                        throw new InvalidOperationException(_localizer["Xncf.BatchUpdate.VersionMismatch", register.Version, updatedModule.Version]);
                    }

                    if (updatedModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                    {
                        updatedModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                        await _xncfModuleServiceEx.SaveObjectAsync(updatedModule).ConfigureAwait(false);
                    }

                    var finalModule = await _xncfModuleServiceEx
                        .GetObjectAsync(z => z.Uid == register.Uid)
                        .ConfigureAwait(false);
                    item.FinalVersion = finalModule?.Version ?? item.FinalVersion;
                    item.FinalState = finalModule == null ? null : (int)finalModule.State;
                    item.EnableSucceeded = finalModule?.State == Ncf.Core.Enums.XncfModules_State.开放;
                    if (!item.EnableSucceeded)
                    {
                        throw new InvalidOperationException(_localizer["Xncf.BatchUpdate.EnableNotApplied"]);
                    }

                    item.Message = _localizer["Xncf.BatchUpdate.UpdateAndEnableSuccess"];
                }
                catch (Exception ex)
                {
                    try
                    {
                        var finalModule = register == null
                            ? null
                            : await _xncfModuleServiceEx.GetObjectAsync(z => z.Uid == register.Uid).ConfigureAwait(false);
                        item.FinalVersion = finalModule?.Version ?? item.FinalVersion;
                        item.FinalState = finalModule == null ? null : (int)finalModule.State;
                        item.EnableSucceeded = item.UpdateSucceeded &&
                            finalModule?.State == Ncf.Core.Enums.XncfModules_State.开放;
                    }
                    catch (Exception statusException)
                    {
                        SenparcTrace.SendCustomLog(
                            "读取批量操作后的 XNCF 模块状态失败",
                            $"模块：{item.ModuleName} / {uid}\r\n{statusException}");
                    }

                    item.Message = item.UpdateSucceeded && item.EnableSucceeded
                        ? _localizer["Xncf.BatchUpdate.UpdateAndEnableSuccess"]
                        : item.UpdateSucceeded
                        ? _localizer["Xncf.BatchUpdate.EnableFailed", ex.Message]
                        : _localizer["Xncf.BatchUpdate.UpdateFailed", ex.Message];

                    SenparcTrace.SendCustomLog(
                        "批量更新并启用 XNCF 模块失败",
                        $"模块：{item.ModuleName} / {uid}\r\n{ex}");
                }

                results.Add(item);
            }

            if (results.Any(z => z.UpdateSucceeded || z.EnableSucceeded))
            {
                try
                {
                    await _sysMenuService.GetMenuDtoByCacheAsync(true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("批量操作后的菜单缓存刷新失败", ex.ToString());
                }
            }

            return Ok(CreateBatchResult(results));
        }

        private static object CreateBatchResult(
            List<BatchUpdateAndEnableXncfModuleResult> results,
            string message = null)
        {
            var successCount = results.Count(z => z.UpdateSucceeded && z.EnableSucceeded);
            return new
            {
                Success = message == null && results.Count > 0 && successCount == results.Count,
                TotalCount = results.Count,
                SuccessCount = successCount,
                FailureCount = results.Count - successCount,
                Message = message,
                Items = results
            };
        }

        /// <summary>
        /// 根据名称安装模块
        /// </summary>
        /// <param name="xncfName"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetInstallModuleAsync(string xncfName)
        {
            bool success = true;
            string message = null;
            if (base.FullSystemConfig.HideModuleManager == true)
            {
                success = false;
                message = _localizer["Xncf.Install.PublishModeEnabled"];
            }
            else
            {
                var docRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Name == xncfName);
                if (docRegister == null)
                {
                    success = false;
                    message = _localizer["Xncf.Install.ModuleNotFound"];
                }
                else
                {
                    try
                    {
                        //查找并安装模块
                        var docModule = await _xncfModuleServiceEx.GetObjectAsync(z => z.Uid == docRegister.Uid);
                        if (docModule == null)
                        {
                            await _xncfModuleServiceEx.InstallModuleAsync(docRegister.Uid, true);
                            docModule = await _xncfModuleServiceEx.GetObjectAsync(z => z.Uid == docRegister.Uid);
                        }
                        //开启模块
                        if (docModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                        {
                            docModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                            await _xncfModuleServiceEx.SaveObjectAsync(docModule);
                        }

                        message = _localizer["Xncf.Install.Success"];
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = _localizer["Xncf.Install.Failed", ex.Message];
                    }
                }
            }

            return new JsonResult(new { success, message });

        }
    }

    public class BatchUpdateAndEnableXncfModulesRequest
    {
        public List<string> Uids { get; set; } = new List<string>();
    }

    public class BatchUpdateAndEnableXncfModuleResult
    {
        public string Uid { get; set; }
        public string ModuleName { get; set; }
        public string PreviousVersion { get; set; }
        public string TargetVersion { get; set; }
        public string FinalVersion { get; set; }
        public bool UpdateSucceeded { get; set; }
        public bool EnableSucceeded { get; set; }
        public int? FinalState { get; set; }
        public string Message { get; set; }
    }
}
