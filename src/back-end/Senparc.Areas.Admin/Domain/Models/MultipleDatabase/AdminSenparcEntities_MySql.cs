using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// 当前 Entities 只为帮助 SenparcEntities 生成 Migration 信息而存在，没有特别的操作意义。
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class AdminSenparcEntities_MySql : AdminSenparcEntities
    {
        public AdminSenparcEntities_MySql(DbContextOptions<AdminSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、将当前项目设为启动项</para>
    /// <para>3、打开【程序包资源管理器控制台】，默认项目设为当前项目</para>
    /// <para>4、运行：PM> add-migration [更新名称] -Context AdminSenparcEntities_MySql -o SystemEntities/Migrations/Migrations.MySql.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<AdminSenparcEntities_MySql, Register>
    {
        protected override Action<IServiceCollection> ServicesAction => services =>
        {
            //指定其他数据库
            services.AddDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDatabaseConfiguration");
        };

        public SenparcDbContextFactory_MySql()
            : base(
                 /* Debug模式下项目根目录
                 /* 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息 */
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\Senparc.Web"))
        {
            Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseName = "Local";//默认配置
        }
    }
}
