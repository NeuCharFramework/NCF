using Microsoft.AspNetCore.Mvc;
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

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class XncfModuleIndexModel : BaseAdminPageModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx;
        private readonly SysMenuService _sysMenuService;

        //TODO:从其他模块获得，或独立到对应模块的API
        private readonly Lazy<SystemConfigService> _systemConfigService;

        public XncfModuleIndexModel(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleServiceEx,
            SysMenuService sysMenuService, Lazy<SystemConfigService> systemConfigService)
        {
            CurrentMenu = "XncfModule";

            this._serviceProvider = serviceProvider;
            this._xncfModuleServiceEx = xncfModuleServiceEx;
            this._sysMenuService = sysMenuService;
            this._systemConfigService = systemConfigService;
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
            var result = await _xncfModuleServiceEx.InstallModuleAsync(uid);
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
        /// 获取已安装模块模块 handler=Mofules
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
        /// 获取未安装模块模块 handler=UnMofules
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetUnMofulesAsync()
        {
            //所有已安装的模块
            var oldXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, z => true, z => z.AddTime, Ncf.Core.Enums.OrderingType.Descending);
            //未安装或版本已更新（不同）的模块
            var newXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(oldXncfModules);

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
            var result = await _xncfModuleServiceEx.InstallModuleAsync(uid);
            //XncfModules = result.Item1;
            //base.SetMessager(Ncf.Core.Enums.MessageType.info, result.Item2, true);
            return Ok(result.XncfModuleList);
            //return RedirectToPage("Index");
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
                message = "已经启用“发布模式”，无法进行此操作";
            }
            else
            {
                var docRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Name == xncfName);
                if (docRegister == null)
                {
                    success = false;
                    message = "文档模块不存在，无法完成安装！";
                }
                else
                {
                    try
                    {
                        //查找并安装模块
                        var docModule = await _xncfModuleServiceEx.GetObjectAsync(z => z.Uid == docRegister.Uid);
                        if (docModule == null)
                        {
                            await _xncfModuleServiceEx.InstallModuleAsync(docRegister.Uid);
                            docModule = await _xncfModuleServiceEx.GetObjectAsync(z => z.Uid == docRegister.Uid);
                        }
                        //开启模块
                        if (docModule.State != Ncf.Core.Enums.XncfModules_State.开放)
                        {
                            docModule.UpdateState(Ncf.Core.Enums.XncfModules_State.开放);
                            await _xncfModuleServiceEx.SaveObjectAsync(docModule);
                        }

                        message = "安装成功！";
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = "安装失败：" + ex.Message;
                    }
                }
            }

            return new JsonResult(new { success, message });

        }
    }
}