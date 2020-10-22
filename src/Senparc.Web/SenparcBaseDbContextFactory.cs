using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System;
using System.IO;

namespace Senparc.Web
{
    //暂时弃用

    ///// <summary>
    ///// 设计时 DbContext 创建，用于更新底层（Senparc.Ncf.Core）的实体，仅供开发时使用
    ///// </summary>
    //public class SenparcBaseDbContextFactory : IDesignTimeDbContextFactory<SenparcEntities>
    //{
    //    public SenparcEntities CreateDbContext(string[] args)
    //    {
    //        //修复 https://github.com/NeuCharFramework/NCF/issues/13 发现的问题（在非Web环境下无法得到网站根目录路径）
    //        IRegisterService register = RegisterService.Start(new SenparcSetting());

    //        SiteConfig.SenparcCoreSetting.DatabaseName = "Local";//指定数据库配置，对应 Appp_Data/Database/SenparcConfig.config 下的配置

    //        CO2NET.Config.RootDictionaryPath = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"); //


    //        //如果运行 Add-Migration 命令，并且获取不到正确的网站根目录，此处可能无法自动获取到连接字符串（上述#13问题），
    //        //也可通过下面已经注释的的提供默认值方式解决（不推荐）
    //        var sqlConnection = SenparcDatabaseConfigs.ClientConnectionString; //?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";

    //        Console.WriteLine("============= 连接字符串 ===============");
    //        Console.WriteLine(SenparcDatabaseConfigs.ClientConnectionString);
    //        Console.WriteLine("========================================");

    //        Service.Register systemServiceRegister = new Service.Register();

    //        XncfRegisterManager.XncfDatabaseList.Add(systemServiceRegister);//添加注册

    //        var builder = new DbContextOptionsBuilder<SenparcEntities>();

    //        builder.UseSqlServer(sqlConnection, b => systemServiceRegister.DbContextOptionsAction(b, "Senparc.Web"));//beta5
    //        return new SenparcEntities(builder.Options);
    //    }
    //}
}
