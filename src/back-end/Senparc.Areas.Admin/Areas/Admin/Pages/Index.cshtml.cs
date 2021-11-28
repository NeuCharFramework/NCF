using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages
{
    [Ncf.AreaBase.Admin.Filters.IgnoreAuth]
    public class IndexModel : BaseAdminPageModel
    {
        private readonly IServiceProvider _serviceProvider;

        //TODO:从其他模块获得
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx;

        public IndexModel(IServiceProvider serviceProvider, XncfModuleServiceExtension xncfModuleServiceEx)
        {
            this._serviceProvider = serviceProvider;
            this._xncfModuleServiceEx = xncfModuleServiceEx;
        }

        public IActionResult OnGet()
        {
            return null;
            //return RedirectToPage("/Home/Index");
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetXncfStatAsync()
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
            var xncfRegisterManager = new XncfRegisterManager(_serviceProvider);
            var missingXncfCount = installedXncfModules.Count(z => !XncfRegisterManager.RegisterList.Exists(r => r.Uid == z.Uid));

            var data = new
            {
                success = true,
                data = new
                {
                    installedXncfCount = installedXncfModules.Count,
                    updateVersionXncfCount,
                    newXncfCount,
                    missingXncfCount
                }
            };
            return new JsonResult(data);
        }

        /// <summary>
        /// 获取开放中的模块信息
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetXncfOpeningAsync()
        {
            //所有已安装模块
            var installedXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, z => z.State == Ncf.Core.Enums.XncfModules_State.开放, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);
            var xncfModuleDtos = installedXncfModules.Select(z =>
            {
                var data = _xncfModuleServiceEx.Mapper.Map<XncfModuleDto>(z);
                //data.Icon = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == z.Uid)?.Icon;
                return data;
            });
            return new JsonResult(new
            {
                success = true,
                data = xncfModuleDtos
            });
        }

        #region 菜单相关


        /// <summary>
        /// 获取无状态的菜单信息
        /// </summary>
        /// <param name="sysPermissionService"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMenuTreeAsync([FromServices] SysPermissionService sysPermissionService)
        {
            IEnumerable<SysMenuDto> sysMenus = await sysPermissionService.GetCurrentUserMenuDtoAsync();
            var results = await GetSysMenuTreesMainRecursiveAsync(sysMenus);
            return Ok(results);
        }

        /// <summary>
        /// 获取包含当前停留页面信息的菜单信息
        /// </summary>
        /// <param name="sysPermissionService"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnGetMenuResourceAsync([FromServices] SysPermissionService sysPermissionService)
        {
            IEnumerable<SysMenuDto> sysMenus = await sysPermissionService.GetCurrentUserMenuDtoAsync();
            var results = await GetSysMenuTreesMainRecursiveAsync(sysMenus);
            var resourceCodes = await sysPermissionService.GetCurrentUserResourcesDtoAsync();
            return Ok(new { menuList = results, resourceCodes });
        }

        /// <summary>
        /// 获取 Admin 后台左侧菜单结构
        /// </summary>
        /// <param name="sysMenuTreeItems"></param>
        /// <returns></returns>
        private async Task<IEnumerable<SysMenuTreeItemDto>> GetSysMenuTreesMainRecursiveAsync(IEnumerable<SysMenuDto> sysMenuTreeItems)
        {
            bool hideModuleManager = FullSystemConfig.HideModuleManager == true;//是否处于发布状态，需要隐藏部分菜单
            List<SysMenuTreeItemDto> sysMenuTrees = new List<SysMenuTreeItemDto>();
            List<SysMenuDto> dest = new List<SysMenuDto>();
            int index = 60000;

            XncfRegisterManager xncfRegisterManager = new XncfRegisterManager(_serviceProvider);

            //遍历菜单设置项目，查找和 XNCF 模块有关的菜单节点
            foreach (var item in sysMenuTreeItems)
            {
                //定位 XNCF 模块
                IXncfRegister xncfRegister = XncfRegisterList
                    .FirstOrDefault(z => !string.IsNullOrEmpty(item.Url) &&
                                         item.Url.Contains($"uid={z.Uid}", StringComparison.OrdinalIgnoreCase)); //TODO:判断Xncf条件还可以更细

                var isStoredXncf = !string.IsNullOrEmpty(item.Url) &&
                                    item.Url.Contains("uid=", StringComparison.OrdinalIgnoreCase);//在数据库里面注册为模块
                var xncfMissing = isStoredXncf && xncfRegister == null;//程序集未加载

                if (xncfRegister != null &&
                    xncfRegister is Senparc.Ncf.Core.Areas.IAreaRegister xncfAreapage &&
                    xncfAreapage.AareaPageMenuItems.Count() > 0)
                {
                    if (hideModuleManager)
                    {
                        item.ParentId = null;
                        item.Id = (index++).ToString();
                    }
                    else
                    {
                        dest.Add(new SysMenuDto()
                        {
                            MenuName = "设置/执行",
                            Url = item.Url,
                            Id = (index++).ToString(),
                            ParentId = item.Id,
                            Icon = "fa fa-play"
                        });
                    }


                    dest.AddRange(xncfAreapage.AareaPageMenuItems.Select(_ => new SysMenuDto()
                    {
                        MenuName = _.Name,
                        Url = _.Url,
                        Icon = _.Icon,
                        Id = string.IsNullOrEmpty(_.Id) ? (index++).ToString() : _.Id,
                        ParentId = string.IsNullOrEmpty(_.ParentId) ? item.Id : _.ParentId
                    }));
                    item.Url = string.Empty;
                }
                else if (!string.IsNullOrEmpty(item.Url) && item.Url.Contains("uid=") && hideModuleManager)//TODO:判断Xncf条件还可以更细
                {
                    item.ParentId = null;
                    item.Id = (index++).ToString();
                }

                if (hideModuleManager && item.Id == "4")
                {
                    continue;//拓展模块，判断是否隐藏
                }

                //该菜单为模块用的菜单
                if (isStoredXncf)
                {
                    if (xncfMissing)
                    {
                        //XNCF 不可用状态下，给以提示（无论是否在管理模式下）
                        item.MenuName = $"!! {item.MenuName} !!";
                    }
                    else
                    {
                        var checkXncfValiable = await xncfRegisterManager.CheckXncfValiable(xncfRegister);
                        if (!checkXncfValiable)
                        {
                            if (hideModuleManager)
                            {
                                //XNCF 不可用状态下，不显示
                                continue;
                            }
                            else
                            {
                                //XNCF 不可用状态下，给以提示
                                item.MenuName = $"~~ {item.MenuName} ~~";
                            }
                        }
                    }
                }

                dest.Add(item);
            }
            GetSysMenuTreesRecursive(dest, sysMenuTrees, null);
            return sysMenuTrees;
        }

        /// <summary>
        /// 递归设置菜单节点信息
        /// </summary>
        /// <param name="sysMenuTreeItems"></param>
        /// <param name="sysMenuTrees"></param>
        /// <param name="parent"></param>
        private void GetSysMenuTreesRecursive(IEnumerable<SysMenuDto> sysMenuTreeItems, IList<SysMenuTreeItemDto> sysMenuTrees, SysMenuDto parent)
        {
            IEnumerable<SysMenuDto> list = sysMenuTreeItems.Where(_ => _.ParentId == parent?.Id);
            foreach (var item in list)
            {
                SysMenuTreeItemDto sysMenu = new SysMenuTreeItemDto()
                {
                    MenuName = item.MenuName,
                    Id = item.Id,
                    Icon = item.Icon,
                    Url = item.Url,
                    Children = new List<SysMenuTreeItemDto>()
                };
                sysMenuTrees.Add(sysMenu);
                GetSysMenuTreesRecursive(sysMenuTreeItems, sysMenu.Children, item);
            }
        }

        #endregion


    }
}