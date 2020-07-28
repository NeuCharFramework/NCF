using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Attributes;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Senparc.Core.Models
{

    public partial class SenparcEntities : SenparcEntitiesBase, ISenparcEntities
    {
        public SenparcEntities(DbContextOptions<SenparcEntities> dbContextOptions) : base(dbContextOptions)
        {
        }


        #region 系统表

        public virtual DbSet<AdminUserInfo> AdminUserInfos { get; set; }

        public DbSet<FeedBack> FeedBacks { get; set; }

        #endregion


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region 系统表

            //实现 [XncfAutoConfigurationMapping] 特性之后，可以自动执行，无需手动添加
            //modelBuilder.ApplyConfiguration(new AdminUserInfoConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new FeedbackConfigurationMapping());

            #endregion

            #region 其他动态模块

            foreach (var databaseRegister in Senparc.Ncf.XncfBase.Register.XncfDatabaseList)
            {
                databaseRegister.OnModelCreating(modelBuilder);
            }

            #endregion

            #region 全局自动注入（请勿改变此命令位置）

            //注册所有 XncfAutoConfigurationMapping 动态模块
            var dt1 = SystemTime.Now;
            Senparc.Ncf.XncfBase.Register.ApplyAllAutoConfigurationMapping(modelBuilder);
            SenparcTrace.SendCustomLog("SenparcEntities 数据库实体注入", $"耗时：{SystemTime.DiffTotalMS(dt1)}ms");

            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}
