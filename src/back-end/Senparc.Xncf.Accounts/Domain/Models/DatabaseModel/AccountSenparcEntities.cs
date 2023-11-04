using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.Accounts.Models
{
    public class AccountSenparcEntities : XncfDatabaseDbContext
    {
        public AccountSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }


        /// <summary>
        /// 用户信息
        /// </summary>
        public virtual DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// 用户支付日志
        /// </summary>

        public virtual DbSet<AccountPayLog> AccountPayLogs { get; set; }

        /// <summary>
        /// 用户积分日志
        /// </summary>
        public virtual DbSet<PointsLog> PointsLogs { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AccountConfigurationMapping());
            modelBuilder.ApplyConfiguration(new AccountPayLogConfigurationMapping());
            modelBuilder.ApplyConfiguration(new PointsLogConfigurationMapping());
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// </summary>
    public class SenparcDbContextFactoryHeler
    {
      
    }
}
