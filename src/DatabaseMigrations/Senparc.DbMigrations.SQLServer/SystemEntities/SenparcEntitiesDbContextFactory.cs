using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Senparc.DbMigrations.SQLServer
{
    /// <summary>
    /// 设计时 DbContext 创建
    /// 用于更新底层（Senparc.Ncf.Core）的实体，仅供开发时使用。
    /// add-migration 操作说明：
    /// 1、将 Senparc.Service 设为启动项目
    /// 2、【程序包管理器控制台】中设置“默认项目”为“Senparc.Service”
    /// 3、PM> add-migration [更新名称] -Context SenparcEntities -OutputDir "SystemEntities/MigrationsForSenparcEntities"
    /// </summary>
    public class SenparcEntitiesDbContextFactory : SenparcDesignTimeDbContextFactoryBase<SenparcEntities>
    {
        Service.Register _systemServiceRegister = new Service.Register();

        XncfDatabaseData XncfDatabaseData { get; }

        public SenparcEntitiesDbContextFactory()
            : base(Senparc.Core.VersionInfo.VERSION,
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\..\\", "Senparc.Web"))
        {
            XncfDatabaseData = new XncfDatabaseData(_systemServiceRegister, null)
            {
                AssemblyName = "Senparc.DbMigrations.SQLServer"//强制指定程序集
            };

            //在 CreateDbContext() 执行之前，指定使用 SQL Server
            //方法一：
            //DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration = new SQLServerDatabaseConfiguration();
            //方法二：在 ServicesAction 中指定
        }

        protected override Action<IServiceCollection> ServicesAction => 
            services => services.AddDatabase<SQLServerDatabaseConfiguration>();//指定其他数据库


        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsAction => (b, d) =>
        {
            d = XncfDatabaseData;
            //_systemServiceRegister.DbContextOptionsAction(b, "Senparc.DbMigrations.SQLServer");
            base.DbContextOptionsAction(b, d);//执行基类中的方法（必须）
        };

        public override void CreateDbContextAction()
        {
            XncfRegisterManager.XncfDatabaseList.Add(_systemServiceRegister);//添加注册
        }


        public override SenparcEntities GetDbContextInstance(DbContextOptions<SenparcEntities> dbContextOptions)
        {
            return new SenparcEntities(dbContextOptions);
        }
    }
}
