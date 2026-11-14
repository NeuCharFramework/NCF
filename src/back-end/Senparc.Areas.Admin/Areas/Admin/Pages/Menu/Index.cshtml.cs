/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml 相关功能实现


    创建标识：Senparc - 20200724

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class MenuIndexModel : BaseAdminPageModel
    {
        private readonly SysMenuService _sysMenuService;

        public MenuIndexModel(IServiceProvider serviceProvider, SysMenuService _sysMenuService)
            : base(serviceProvider)
        {
            CurrentMenu = "Menu";
            this._sysMenuService = _sysMenuService;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 
        /// </summary>
        public PagedList<SysMenu> SysMenus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task OnGetAsync()
        {
            SysMenus = await _sysMenuService.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
        }

        public IActionResult OnPostDelete(string[] ids)
        {
            foreach (var id in ids)
            {
                _sysMenuService.DeleteObject(_ => _.Id == id);
            }

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// 配置模式：保存整棵树“菜单/页面”的拖拽/数字排序结果，按父级分组真实更新 Sort 数值
        /// </summary>
        /// <param name="request">按从上到下（深度优先）顺序排列的“菜单/页面”Id 列表</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostReorderAsync([FromBody] MenuReorderRequest request)
        {
            if (request?.Ids == null || request.Ids.Count == 0)
            {
                return Ok(false, "排序数据不能为空");
            }

            var validIds = request.Ids
                .Where(z => !string.IsNullOrEmpty(z))
                .Distinct()
                .ToList();

            if (validIds.Count == 0)
            {
                return Ok(false, "排序数据不能为空");
            }

            var menus = (await _sysMenuService.GetFullListAsync(_ => validIds.Contains(_.Id))).ToList();
            if (menus.Count == 0)
            {
                return Ok(false, "未找到可排序的菜单");
            }

            // 仅处理“菜单/页面”类型（前端只提交这两类，这里做兜底），按钮类型不参与排序
            var sortableMenus = menus
                .Where(_ => _.MenuType == MenuType.菜单 || _.MenuType == MenuType.页面)
                .ToList();

            // 按父级分组：同一父级下的子节点按提交顺序（从上到下）分配 Sort 数值
            var parentGroups = new Dictionary<string, List<(int Index, SysMenu Menu)>>();
            foreach (var menu in sortableMenus)
            {
                var parentKey = menu.ParentId ?? string.Empty;
                if (!parentGroups.TryGetValue(parentKey, out var group))
                {
                    group = new List<(int, SysMenu)>();
                    parentGroups[parentKey] = group;
                }
                group.Add((validIds.IndexOf(menu.Id), menu));
            }

            var toUpdate = new List<SysMenu>();
            foreach (var parentGroup in parentGroups)
            {
                var ordered = parentGroup.Value.OrderBy(z => z.Index).ToList();
                int total = ordered.Count;
                for (int i = 0; i < ordered.Count; i++)
                {
                    var menu = ordered[i].Menu;
                    // 左侧菜单按 Sort 降序渲染：位置越靠前（index 越小），Sort 数值越大
                    int newSort = (total - i) * 10;
                    if (menu.Sort != newSort)
                    {
                        menu.Sort = newSort;
                        menu.LastUpdateTime = DateTime.Now;
                        toUpdate.Add(menu);
                    }
                }
            }

            if (toUpdate.Count > 0)
            {
                await _sysMenuService.SaveObjectListAsync(toUpdate);
            }

            // 刷新菜单缓存，使左侧菜单顺序立即生效
            await _sysMenuService.GetMenuDtoByCacheAsync(true);

            return Ok(true);
        }
    }

    /// <summary>
    /// 菜单排序请求（按深度优先顺序提交全部“菜单/页面”节点 Id）
    /// </summary>
    public class MenuReorderRequest
    {
        public List<string> Ids { get; set; }
    }
}