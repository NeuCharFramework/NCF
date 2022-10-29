using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.AuditLog.Models;
using Senparc.Xncf.AuditLog.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.AuditLog.Models.DatabaseModel.Dto;

using DemoAudit.Filters;
using DemoAudit.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Senparc.Xncf.AuditLog.Domain.Services;

namespace Senparc.Xncf.AuditLog
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.AuditLog";

        public override string Uid => "61D0E6EF-B90F-417F-9032-826EBE163228";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "1.1";//必须填写版本号

        public override string MenuName => "审计日志";

        public override string Icon => "fa";

        public override string Description => "与安全相关的按照时间顺序的记录，记录集或者记录源";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //安装或升级版本时更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //根据安装或更新不同条件执行逻辑
            switch (installOrUpdate)
            {
                case InstallOrUpdate.Install:
                    //新安装
            #region 初始化数据库数据
                    var auditLogService = serviceProvider.GetService<AuditLogService>();
            #endregion
                    break;
                case InstallOrUpdate.Update:
                    //更新
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AuditLogSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AuditLogSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //services.AddScoped<ColorAppService>();
            //services.AddScoped<AuditFilterAttribute>();

            services.AddTransient<IAuditRepository, AuditRepository>();
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add(typeof(AuditFilterAttribute));
            });

            services.AddScoped<AuditFilterAttribute>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            return base.AddXncfModule(services, configuration, env);
        }
    }
}
