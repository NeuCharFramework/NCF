using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Database;
using System;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// 当前 Entities 只为帮助 SenparcEntities 生成 Migration 信息而存在，没有特别的操作意义。
    /// </summary>
    public class AdminSenparcEntities : XncfDatabaseDbContext
    {
        public AdminSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        #region 系统表（无特殊情况不要修改）

        /// <summary>
        /// 系统设置
        /// </summary>
        public DbSet<AdminUserInfo> SystemConfigs { get; set; }

        /// <summary>
        /// 管理后台聊天会话
        /// </summary>
        public DbSet<AdminChatSession> AdminChatSessions { get; set; }

        /// <summary>
        /// 管理后台聊天消息
        /// </summary>
        public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

        /// <summary>
        /// 管理后台聊天会话-模块关联
        /// </summary>
        public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point

        #endregion
    }
}
