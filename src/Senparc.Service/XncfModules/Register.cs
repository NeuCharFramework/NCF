/* 
 * 特别注意：
 * 当前注册类是比较特殊的底层系统支持模块，
 * 其中加入了一系列特殊处理的代码，并不适合所有模块使用，
 * 如果需要学习扩展模块，请参考 【Senparc.ExtensionAreaTemplate】 项目的 Register.cs 文件！
 */

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Service
{
    public class Register : XncfRegisterBase,
         IXncfRegister, //注册 XNCF 基础模块接口（必须）
         IXncfDatabase  //注册 XNCF 模块数据库（按需选用）
    {
        #region IXncfRegister 接口

        public override string Name => "NeuCharFramework.Services";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_SERVICE_UID;// "00000000-0000-0000-0000-000000000001";

        public override string Version => "0.3.2-beta4";

        public override string MenuName => "NCF 系统服务运行核心";

        public override string Icon => "fa fa-university";

        public override string Description => "这是系统服务核心模块，主管基础数据结构和网站核心运行数据，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";

        public override IList<Type> Functions => new Type[] { };


        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<XncfModuleServiceExtension>();
            services.AddScoped<SystemServiceEntities>();

            return base.AddXncfModule(services, configuration);
        }

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //TODO：DI注入注册时候，根据指定数据库进行绑定

            XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;

            //更新数据库
            var pendingMigs = await senparcEntities.Database.GetPendingMigrationsAsync();
            if (pendingMigs.Count() > 0)
            {
                senparcEntities.ResetMigrate();//重置合并状态

                try
                {
                    var script = senparcEntities.Database.GenerateCreateScript();
                    SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

                    senparcEntities.Migrate();//进行合并
                }
                catch (Exception ex)
                {
                    var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                    SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), senparcEntities.GetType(), ex));
                }
            }

            //更新数据库（目前不使用 SystemServiceEntities 存放数据库模型）
            //await base.MigrateDatabaseAsync<SystemServiceEntities>(serviceProvider);

            var systemModule = xncfModuleServiceExtension.GetObject(z => z.Uid == this.Uid);
            if (systemModule == null)
            {
                //只在未安装的情况下进行安装，InstallModuleAsync会访问到此方法，不做判断可能会引发死循环。
                //常规模块中请勿在此方法中自动安装模块！
                await xncfModuleServiceExtension.InstallModuleAsync(this.Uid).ConfigureAwait(false);
            }

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO：应该提供一个 BeforeUninstall 方法，阻止卸载。

            return base.UninstallAsync(serviceProvider, unsinstallFunc);
        }

        #endregion

        #region IXncfDatabase 接口

        public string DatabaseUniquePrefix => "SystemService";//特殊情况：没有前缀
        public Type XncfDatabaseDbContextType => typeof(SystemServiceEntities);

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //已在 SenparcEntities 中完成
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            #region 历史解决方案参考信息
            /* 参考信息
             *      错误信息：
             *          中文：EnableRetryOnFailure 解决短暂的数据库连接失败
             *          英文：Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
             *      问题解决方案说明：https://www.colabug.com/2329124.html
             */

            /* 参考信息
             *      错误信息：
             *          中文：EnableRetryOnFailure 解决短暂的数据库连接失败
             *          英文：Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
             *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
             *      问题解决方案说明：https://www.colabug.com/2329124.html
             */
            #endregion

            var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;

            /* 
             *     非常重要！！
             * SenparcEntities 工厂配置
             */
            var xncfDatabaseData = new XncfDatabaseData(this, "Senparc.Service"/*从当前程序集读取*/);

            #region 不属于任何模块

            //这个配置面相基类，不属于任何模块
            Func<IServiceProvider, SenparcEntitiesMultiTenant> multiTenantImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetDbContext<SenparcEntitiesMultiTenant>(serviceProvider: s);
            };
            services.AddScoped<SenparcEntitiesMultiTenant>(multiTenantImplementationFactory);//继承自 SenparcEntitiesMultiTenantBase

            Func<IServiceProvider, SenparcEntities> senparcEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;

                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as SenparcEntities;
            };

            services.AddScoped<SenparcEntitiesDbContextBase>(senparcEntitiesImplementationFactory);// 继承自 DbContext
            services.AddScoped<ISenparcEntitiesDbContext>(senparcEntitiesImplementationFactory);
            services.AddScoped<SenparcEntitiesBase>(senparcEntitiesImplementationFactory);//继承自 SenparcEntitiesMultiTenantBase
            services.AddScoped<SenparcEntities>(senparcEntitiesImplementationFactory);

            #endregion

            //SystemServiceEntities 工厂配置（实际不会用到）
            Func<IServiceProvider, SystemServiceEntities> systemServiceEntitiesImplementationFactory = s =>
            {
                var multipleDatabasePool = MultipleDatabasePool.Instance;
                return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as SystemServiceEntities;
            };
            services.AddScoped<SystemServiceEntities>(systemServiceEntitiesImplementationFactory);

            services.AddScoped(typeof(INcfClientDbData), typeof(NcfClientDbData));
            services.AddScoped(typeof(INcfDbData), typeof(NcfClientDbData));

            //预加载 EntitySetKey
            EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntities));
        }


        #endregion

        #region 扩展

        public async Task<(bool success, string msg)> InitDatabase(IServiceProvider serviceProvider, TenantInfoService tenantInfoService, HttpContext httpContext)
        {
            var success = true;
            string msg = null;

            //SenparcEntities senparcEntities = (SenparcEntities)xncfModuleServiceExtension.BaseData.BaseDB.BaseDataContext;
            using (var scope = serviceProvider.CreateScope())
            {
                //暂时关闭多租户状态
                SiteConfig.SenparcCoreSetting.EnableMultiTenant = false;

                SenparcEntities senparcEntities = scope.ServiceProvider.GetRequiredService<SenparcEntities>();
                //更新数据库
                var pendingMigs = await senparcEntities.Database.GetPendingMigrationsAsync();
                if (pendingMigs.Count() > 0)
                {
                    senparcEntities.ResetMigrate();//重置合并状态

                    try
                    {

                        var script = senparcEntities.Database.GenerateCreateScript();
                        SenparcTrace.SendCustomLog("senparcEntities.Database.GenerateCreateScript", script);

                        senparcEntities.Migrate();//进行合并

                    }
                    catch (Exception ex)
                    {
                        success = false;
                        msg = ex.Message;

                        var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                        SenparcTrace.BaseExceptionLog(new NcfDatabaseException(ex.Message, currentDatabaseConfiguration.GetType(), senparcEntities.GetType(), ex));
                    }
                }

                SiteConfig.SenparcCoreSetting.EnableMultiTenant = true;
            }

            return (success: success, msg: msg);
        }

        #endregion
    }
}
