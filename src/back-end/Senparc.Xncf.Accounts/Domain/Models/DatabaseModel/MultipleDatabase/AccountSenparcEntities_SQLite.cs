﻿
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Ncf.Core.Models;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.Accounts.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class AccountSenparcEntities_Sqlite : AccountSenparcEntities
    {
        public AccountSenparcEntities_Sqlite(DbContextOptions<AccountSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    /// <para>1、切换至 Debug 模式</para>
    /// <para>2、运行：PM> add-migration [更新名称] -c AccountSenparcEntities_Sqlite -o Domain/Migrations/Migrations.Sqlite </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<AccountSenparcEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //指定其他数据库
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
