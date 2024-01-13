
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using SenparcConf.Xncf.WechatBot.Models.DatabaseModel;

namespace SenparcConf.Xncf.WechatBot.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class WechatBotSenparcEntities_PostgreSQL : WechatBotSenparcEntities
    {
        public WechatBotSenparcEntities_PostgreSQL(DbContextOptions<WechatBotSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c WechatBotSenparcEntities_PostgreSQL -o Migrations/Migrations.PostgreSQL </para>
    /// </summary>
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<WechatBotSenparcEntities_PostgreSQL, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
