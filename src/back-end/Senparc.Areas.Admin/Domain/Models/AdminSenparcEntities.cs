using Microsoft.EntityFrameworkCore;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// 当前 Entities 只为帮助 SenparcEntities 生成 Migration 信息而存在，没有特别的操作意义。
    /// </summary>
    public class AdminSenparcEntities : SenparcEntities
    {
        public AdminSenparcEntities(DbContextOptions/*<SystemServiceEntities>*/ dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }

        #region 系统表（无特殊情况不要修改）

        /// <summary>
        /// 系统设置
        /// </summary>
        public DbSet<AdminUserInfo> SystemConfigs { get; set; }

        #endregion
    }
}
