
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.WeixinManagerBase.Models.DatabaseModel;

namespace Senparc.Xncf.WeixinManagerBase.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class WeixinManagerBaseSenparcEntities_MySql : WeixinManagerBaseSenparcEntities
    {
        public WeixinManagerBaseSenparcEntities_MySql(DbContextOptions<WeixinManagerBaseSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c WeixinManagerBaseSenparcEntities_MySql -o Domain/Migrations/Migrations.MySql </para>
    /// </summary>
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<WeixinManagerBaseSenparcEntities_MySql, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDatabaseConfiguration");
        };

        public SenparcDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
