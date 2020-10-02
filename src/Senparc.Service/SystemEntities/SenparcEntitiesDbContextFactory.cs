using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
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
    /// 3、PM> add-migration [更新名称] -Context SenparcEntities -OutputDir "SystemEntities/MigrationsForSenparcEntities"
    /// </summary>
    public class SenparcEntitiesDbContextFactory : SenparcDesignTimeDbContextFactoryBase<SenparcEntities>
    {
        Service.Register _systemServiceRegister = new Service.Register();

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure> DbContextOptionsAction => b =>
        {
            base.DbContextOptionsAction(b);//执行基类中的方法（必须）
            _systemServiceRegister.DbContextOptionsAction(b, "Senparc.Service");//自定义配置
        };

        public SenparcEntitiesDbContextFactory()
            : base(Senparc.Core.VersionInfo.VERSION,
                 Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\", "..\\Senparc.Web"))
        { 
        }

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
