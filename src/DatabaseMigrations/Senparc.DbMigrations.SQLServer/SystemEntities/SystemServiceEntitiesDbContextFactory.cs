using Senparc.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.DbMigrations.SQLServer
{
    ///// <summary>
    ///// 设计时 DbContext 创建（仅在开发时创建 Code-First 的数据库 Migration 使用，在生产环境不会执行）
    ///// 用于创建当前模块数据库
    ///// PM> Add-Migration AddTables -Context SystemServiceEntities -OutputDir "SystemEntities/MigrationsForSystemServiceEntities"
    ///// </summary>
    //[Obsolete("SystemServiceEntities 暂时不提供实际的类型，系统数据库模型统一由  SenparcEntities 提供。")]
    //public class SystemServiceEntitiesDbContextFactory : SenparcDesignTimeDbContextFactoryBase<SystemServiceEntities, Register>
    //{
    //    /// <summary>
    //    /// 用于寻找 App_Data 文件夹，从而找到数据库连接字符串配置信息
    //    /// </summary>
    //    public override string RootDictionaryPath => Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\", "..\\Senparc.Web"/* Debug模式下项目根目录 */);
    //}
}