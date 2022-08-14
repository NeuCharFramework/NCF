using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.WeixinManagerBase.Models;
using Senparc.Xncf.WeixinManagerBase.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.WeixinManagerBase.Models.DatabaseModel.Dto;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin.RegisterServices;
using Senparc.Weixin.AspNet;
using Senparc.Xncf.WeixinManagerBase.Domain.Services;

namespace Senparc.Xncf.WeixinManagerBase
{
    [XncfOrder(4000)]
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.WeixinManagerBase";

        public override string Uid => "C888A082-3CDA-4682-912F-AE86BAC29F01";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.2.1";//必须填写版本号

        public override string MenuName => "微信公众号";

        public override string Icon => "fa fa-star";

        public override string Description => "微信管理后台基座";

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
                    var colorService = serviceProvider.GetService<ColorAppService>();
                    var colorResult = await colorService.GetOrInitColorAsync();
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
            WeixinManagerBaseSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as WeixinManagerBaseSenparcEntities;

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
            services.AddScoped<ColorAppService>();
            services.AddScoped<UserService>();
            services.AddScoped<WeixinRegisterService>();

            //添加微信服务
            services.AddSenparcWeixinServices(configuration);//Senparc.Weixin 注册（必须）


            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            IHostEnvironment env;

            using (var scope = app.ApplicationServices.CreateScope())
            {
                env = scope.ServiceProvider.GetService<IHostEnvironment>();
            }

            //启用微信配置（必须）
            var weixinRegisterService = app.UseSenparcWeixin(env,
                null /* 不为 null 则覆盖 appsettings  中的 SenpacSetting 配置*/,
                null /* 不为 null 则覆盖 appsettings  中的 SenpacWeixinSetting 配置*/,
                register => { /* CO2NET 全局配置 */ },
                (register, weixinSetting) =>
                {
                    //遍历所有的注册
                    foreach (var weixinRegister in WeixinRegisterService.WeixinRegisterList)
                    {
                        weixinRegister(register, weixinSetting);
                    }
                });

            return base.UseXncfModule(app, registerService);
        }
    }
}
