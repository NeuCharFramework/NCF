using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 菜单
    /// </summary>
    public class SysMenuService : BaseClientService<SysMenu>
    {
        private readonly ISysMenuRepository _repo;
        public SysMenuService(ISysMenuRepository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _repo = repo;
        }

        /// <summary>
        /// 递归获取树形结构
        /// </summary>
        /// <param name="iteration"></param>
        /// <param name="items"></param>
        /// <param name="source"></param>
        private void BuildTreeItems(IEnumerable<SysMenuDto> iteration, IList<SysMenuTreeItemDto> items, IEnumerable<SysMenuDto> source)
        {
            foreach (var menu in iteration)
            {
                var childs = source.Where(_ => _.ParentId == menu.Id).OrderByDescending(o => o.Sort);
                var p = new SysMenuTreeItemDto()
                {
                    Icon = menu.Icon,
                    Id = menu.Id,
                    IsMenu = menu.MenuType == MenuType.菜单,
                    MenuName = menu.MenuName,
                    Url = menu.Url
                };
                p.Children = childs.Select(c => new SysMenuTreeItemDto()
                {
                    Icon = c.Icon,
                    Id = c.Id,
                    IsMenu = c.MenuType == MenuType.菜单,
                    MenuName = c.MenuName,
                    Children = Array.Empty<SysMenuTreeItemDto>(),
                    Url = c.Url
                }).ToList();
                items.Add(p);
                BuildTreeItems(childs, p.Children, source);
            }
        }

        /// <summary>
        /// 获取所有菜单(不包含页面)
        /// TODO... 缓存
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetAllMenusTreeAsync(bool hasButton)
        {
            var allMenus = await _repo.GetAllMenuDtosAsync(hasButton);
            IList<SysMenuTreeItemDto> items = new List<SysMenuTreeItemDto>(allMenus.Count() / 2);
            var iteration = allMenus.Where(_ => _.ParentId == null || _.ParentId == string.Empty).OrderByDescending(o => o.Sort);
            BuildTreeItems(iteration, items, allMenus);
            return items;
        }

        /// <summary>
        /// 获取完整菜单信息
        /// </summary>
        /// <param name="hasButton">是否包含按钮信息</param>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetAllMenuListAsync(bool hasButton)
        {
            return await _repo.GetAllMenuDtosAsync(hasButton);
        }
    }
}
