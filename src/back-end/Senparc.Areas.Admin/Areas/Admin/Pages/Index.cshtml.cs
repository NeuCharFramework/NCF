using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.WorkContext.Provider;
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
    public class IndexModel(
        IServiceProvider serviceProvider,
        XncfModuleServiceExtension xncfModuleServiceEx,
        IAdminWorkContextProvider adminWorkContextProvider)
        : BaseAdminPageModel(serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IAdminWorkContextProvider _adminWorkContextProvider = adminWorkContextProvider;

        //TODO:从其他模块获得
        private readonly XncfModuleServiceExtension _xncfModuleServiceEx = xncfModuleServiceEx;

        /// <summary>
        /// 当前用户ID（可选，用于前端获取）
        /// </summary>
        public int CurrentUserId { get; set; }

        public IActionResult OnGet()
        {
            CurrentUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
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
            var installedXncfModules = await _xncfModuleServiceEx.GetObjectListAsync(0, 0, 
                z => z.State == Ncf.Core.Enums.XncfModules_State.开放, 
                z => z.Id, 
                Ncf.Core.Enums.OrderingType.Descending);

            //获取未安装或待升级模块
            var updateXncfRegisters = _xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules);

            var xncfModuleDtos = installedXncfModules.Select(z =>
            {
                var data = _xncfModuleServiceEx.Mapper.Map<XncfModuleDisplayDto>(z);

                //获取模块的菜单信息
                IXncfRegister xncfRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == data.Uid);
                // if (xncfRegister == null)
                // {
                //     throw new Exception($"模块丢失或未加载（{XncfRegisterManager.RegisterList.Count}）！");
                // }
                data.Menus = (xncfRegister as Ncf.Core.Areas.IAreaRegister)?.AreaPageMenuItems ?? new List<Ncf.Core.Areas.AreaPageMenuItem>();

                //获取模块的Functions信息
                if (xncfRegister != null)
                {
                    var registerType = xncfRegister.GetType();
                    var functionsByModule = Senparc.Ncf.XncfBase.Register.FunctionRenderCollection?.TryGetValue(registerType, out var funcs) == true ? funcs : null;
                    if (functionsByModule != null && functionsByModule.Count > 0)
                    {
                        data.Functions = functionsByModule.Values.Select(f => new
                        {
                            f.FunctionRenderAttribute.Name,
                            f.FunctionRenderAttribute.Description
                        }).ToList();
                    }
                    else
                    {
                        data.Functions = new List<object>();
                    }
                }

                //查找对应的更新版本
                var register = updateXncfRegisters.FirstOrDefault(r => r.Uid == z.Uid);
                if (register != null)
                {
                    var versionDisplay = _xncfModuleServiceEx.GetVersionDisplayName(installedXncfModules, register);
                    if (versionDisplay.Contains("->"))
                    {
                        data.HasNewVersion = true;
                        data.NewVersion = versionDisplay.Split("->")[1].Trim();
                    }
                }

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
        public async Task<IActionResult> OnGetMenuTreeAsync([FromServices] SysRolePermissionService sysPermissionService)
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
        public async Task<IActionResult> OnGetMenuResourceAsync([FromServices] SysRolePermissionService sysPermissionService)
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
                    xncfAreapage.AreaPageMenuItems.Count() > 0)
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


                    dest.AddRange(xncfAreapage.AreaPageMenuItems.Select(_ => new SysMenuDto()
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
                        var CheckXncfAvailable = await xncfRegisterManager.CheckXncfAvailable(xncfRegister);
                        if (!CheckXncfAvailable)
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

            var systemManagementMenu = dest.FirstOrDefault(z =>
                z.ParentId == null &&
                (string.Equals(z.MenuName, "系统管理", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(z.MenuName, "System Management", StringComparison.OrdinalIgnoreCase)))
                ?? dest.FirstOrDefault(z =>
                    z.ParentId == null &&
                    dest.Any(child => child.ParentId == z.Id && string.Equals(child.MenuName, "管理员管理", StringComparison.OrdinalIgnoreCase)));

            var adminChatParentId = systemManagementMenu?.Id;
            var hasAdminChatMenu = dest.Any(z => string.Equals(z.Url, "/Admin/AdminChat/Chat", StringComparison.OrdinalIgnoreCase));

            if (!hasAdminChatMenu)
            {
                dest.Insert(0,new SysMenuDto()
                {
                    MenuName = "AI 智能助手",
                    Url = "/Admin/AdminChat/Chat",
                    Icon = "fa fa-comments-o",
                    Id = (index++).ToString(),
                    ParentId = adminChatParentId
                });
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