using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.ACL.Repository
{
    public interface ISysMenuRepository : IClientRepositoryBase<SysMenu>
    {
        /// <summary>
        /// 获取用户可见菜单
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        Task<IEnumerable<SysMenuDto>> GetVisibleMenuDtosAsync(int accountId);

        /// <summary>
        /// 获取所有菜单(仅包含菜单 和 页面)
        /// <param name="hasButton"></param>
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SysMenuDto>> GetAllMenuDtosAsync(bool hasButton);
    }

    public class SysMenuRepository : ClientRepositoryBase<SysMenu>, ISysMenuRepository
    {
        public SysMenuRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }

        /// <summary>
        /// 获取用户可见的菜单
        /// </summary>
        /// <param name="hasButton"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetAllMenuDtosAsync(bool hasButton)
        {
            var query = BaseDB.BaseDataContext.Set<SysMenu>().Where(o => true);
            if (!hasButton)
            {
                query = query.Where(menu => menu.MenuType < MenuType.按钮); // 获取可见的菜单
            }
            var menus = query.Select(menu => new SysMenuDto()
            {
                Id = menu.Id,
                MenuType = menu.MenuType,
                AddTime = menu.AddTime,
                AdminRemark = menu.AdminRemark,
                Flag = menu.Flag,
                Icon = menu.Icon,
                IsLocked = menu.IsLocked,
                LastUpdateTime = menu.LastUpdateTime,
                MenuName = menu.MenuName,
                ParentId = menu.ParentId,
                Remark = menu.Remark,
                ResourceCode = menu.ResourceCode,
                Sort = menu.Sort,
                Url = menu.Url,
                Visible = menu.Visible
            });
            ;
            return await menus.ToListAsync();
        }

        /// <summary>
        /// 获取用户可见的菜单
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetVisibleMenuDtosAsync(int accountId)
        {
            var sysRoleAdminUserInfos = BaseDB.BaseDataContext.Set<SysRoleAdminUserInfo>()
                .Where(sysRoleAdminUserInfo => sysRoleAdminUserInfo.AccountId == accountId); // 获取用户的角色信息
            var sysRoles = BaseDB.BaseDataContext.Set<SysRole>().Where(role => role.Enabled); // 获取有效的角色信息
            IQueryable<string> roleIdQuery = sysRoleAdminUserInfos.Select(_ => _.RoleId); // 获取用户的角色Id
            var permissions = BaseDB.BaseDataContext.Set<SysRolePermission>().Where(rolePermission => roleIdQuery.Contains(rolePermission.RoleId));
            IQueryable<string> menuIds = permissions.Select(o => o.PermissionId);
            var menus = BaseDB.BaseDataContext.Set<SysMenu>()
                .Where(menu => menu.Visible && menuIds.Contains(menu.Id) && menu.MenuType == MenuType.菜单) // 获取可见的菜单
                .Select(menu => new SysMenuDto()
                {
                    Id = menu.Id,
                    MenuType = menu.MenuType,
                    AddTime = menu.AddTime,
                    AdminRemark = menu.AdminRemark,
                    Flag = menu.Flag,
                    Icon = menu.Icon,
                    IsLocked = menu.IsLocked,
                    LastUpdateTime = menu.LastUpdateTime,
                    MenuName = menu.MenuName,
                    ParentId = menu.ParentId,
                    Remark = menu.Remark,
                    ResourceCode = menu.ResourceCode,
                    Sort = menu.Sort,
                    Url = menu.Url,
                    Visible = menu.Visible
                });
            ;
            return await menus.ToListAsync();
        }
    }
}
