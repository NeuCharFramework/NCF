using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Config;
using System;
using System.IO;

namespace Senparc.Service
{
    /// <summary>
    /// 设计时 DbContext 创建
    /// 用于更新底层（Senparc.Ncf.Core）的实体，仅供开发时使用。
    /// add-migration 操作说明：
    /// 1、将 Senparc.Service 设为启动项目
    /// 2、【程序包管理器控制台】中设置“默认项目”为“Senparc.Service”
    /// 3、PM> add-migration Update_SystemConfig -Context SenparcEntities -OutputDir "SystemEntities/MigrationsForSenparcEntities"
    /// </summary>
    public class SenparcEntitiesDbContextFactory : IDesignTimeDbContextFactory<SenparcEntities>
    {
        public SenparcEntities CreateDbContext(string[] args)
        {
            SiteConfig.SenparcCoreSetting.DatabaseName = "Local";//指定数据库配置，对应 Appp_Data/Database/SenparcConfig.config 下的配置

            CO2NET.Config.RootDictionaryPath = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\","..\\Senparc.Web"); //

            var repository = LogManager.CreateRepository("NETCoreRepository");
            var serviceCollection = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            var senparcSetting = new SenparcSetting() { IsDebug = true };
            serviceCollection.AddSenparcGlobalServices(config);
            serviceCollection.AddMemoryCache();//使用内存缓存
            //修复 https://github.com/SenparcCoreFramework/NCF/issues/13 发现的问题（在非Web环境下无法得到网站根目录路径）
            IRegisterService register = RegisterService.Start(senparcSetting);

            //如果运行 Add-Migration 命令，并且获取不到正确的网站根目录，此处可能无法自动获取到连接字符串（上述#13问题），
            //也可通过下面已经注释的的提供默认值方式解决（不推荐）
            var sqlConnection = SenparcDatabaseConfigs.ClientConnectionString; //?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

            Console.WriteLine("============================");
            Console.WriteLine(SenparcDatabaseConfigs.ClientConnectionString);
            Console.WriteLine("============================");

            Service.Register systemServiceRegister = new Service.Register();

            Senparc.Ncf.XncfBase.Register.XncfDatabaseList.Add(systemServiceRegister);//添加注册

            var builder = new DbContextOptionsBuilder<SenparcEntities>();

            //builder.UseSqlServer(sqlConnection, b => systemServiceRegister.DbContextOptionsAction(b, "Senparc.Web"));//beta4
            builder.UseSqlServer(sqlConnection, b => systemServiceRegister.DbContextOptionsAction(b, "Senparc.Service"));//beta5
            return new SenparcEntities(builder.Options);
        }
    }
}
