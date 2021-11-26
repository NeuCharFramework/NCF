using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// 当前 Entities 只为帮助 SenparcEntities 生成 Migration 信息而存在，没有特别的操作意义。
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class AdminSenparcEntities_PostgreSQL : AdminSenparcEntities
    {
        public AdminSenparcEntities_PostgreSQL(DbContextOptions<AdminSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、将当前项目设为启动项</para>
    /// <para>3、打开【程序包资源管理器控制台】，默认项目设为当前项目</para>
    /// <para>4、运行：PM> add-migration [更新名称] -Context AdminSenparcEntities_PostgreSQL -o SystemEntities/Migrations/Migrations.PostgreSQL.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<AdminSenparcEntities_PostgreSQL, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL()
            : base(
                 /* Debug模式下项目根目录
                 /* 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息 */
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\Senparc.Web"))
        {

        }
    }
}
