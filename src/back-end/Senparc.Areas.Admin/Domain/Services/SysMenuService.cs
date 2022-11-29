using Senparc.Areas.Admin.ACL.Repository;
using Senparc.CO2NET.Extensions;
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
        /// <param name="iteration">当前筛选列表</param>
        /// <param name="source">完整列表数据</param>
        private IList<SysMenuTreeItemDto> BuildTreeItems(IEnumerable<SysMenuDto> iteration, IEnumerable<SysMenuDto> source)
        {
            var items = new List<SysMenuTreeItemDto>();
            foreach (var menu in iteration)
            {
                //查找子菜单
                var parentNode = new SysMenuTreeItemDto()
                {
                    Icon = menu.Icon,
                    Id = menu.Id,
                    IsMenu = menu.MenuType == MenuType.菜单,
                    MenuName = menu.MenuName,
                    Url = menu.Url,
                    Children = new List<SysMenuTreeItemDto>()
                };

                var children = source.Where(z => z.ParentId == menu.Id)
                                     .OrderByDescending(o => o.Sort)
                                     .ToList();
                parentNode.Children = BuildTreeItems(children, source);
                items.Add(parentNode);
            }
            return items;
        }

        /// <summary>
        /// 获取所有菜单(不包含页面)
        /// TODO... 缓存
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetAllMenusTreeAsync(bool hasButton)
        {
            var allMenus = await _repo.GetAllMenuDtosAsync(hasButton);
            var iteration = allMenus.Where(z => z.ParentId.IsNullOrEmpty())
                                    .OrderByDescending(o => o.Sort)
                                    .ToList();
            var items = BuildTreeItems(iteration, allMenus);
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
