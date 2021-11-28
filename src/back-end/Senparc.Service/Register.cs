/* 
 * 特别注意：
 * 当前注册类是比较特殊的底层系统支持模块，
 * 其中加入了一系列特殊处理的代码，并不适合所有模块使用，
 * 如果需要学习扩展模块，请参考 【Senparc.ExtensionAreaTemplate】 项目的 Register.cs 文件！
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Service
{
    public class Register : XncfRegisterBase,
         IXncfRegister, //注册 XNCF 基础模块接口（必须）
         IXncfDatabase  //注册 XNCF 模块数据库（按需选用）
    {
        #region IXncfRegister 接口

        public override string Name => "NeuCharFramework.Services";

        public override string Uid => "10000000-0000-0000-0000-000000000001";// SiteConfig.SYSTEM_XNCF_MODULE_SERVICE_UID;// "00000000-0000-0000-0000-000000000001";

        public override string Version => "0.5.0-beta4";

        public override string MenuName => "NCF 系统服务运行核心";

        public override string Icon => "fa fa-university";

        public override string Description => "这是系统服务核心模块，主管基础数据结构和网站核心运行数据，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<BasePoolEntities>();

            return base.AddXncfModule(services, configuration);
        }

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
           
        }

        public override Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            
        }

        #endregion

        #region IXncfDatabase 接口

        public string DatabaseUniquePrefix => Senparc.Ncf.Core.Models.NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//特殊情况：没有前缀
        public Type XncfDatabaseDbContextType => typeof(BasePoolEntities);

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //已在 SenparcEntities 中完成
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
        
        }


        #endregion


    }
}
