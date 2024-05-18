
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.AgentsManager.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class AgentsManagerSenparcEntities_Sqlite : AgentsManagerSenparcEntities
    {
        public AgentsManagerSenparcEntities_Sqlite(DbContextOptions<AgentsManagerSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c AgentsManagerSenparcEntities_Sqlite -o Domain/Migrations/Migrations.Sqlite </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<AgentsManagerSenparcEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
