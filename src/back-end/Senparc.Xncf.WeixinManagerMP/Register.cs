using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Senparc.Xncf.WeixinManagerMP.Models;
using Senparc.Xncf.WeixinManagerMP.OHS.Local.AppService;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.WeixinManagerMP.Models.DatabaseModel.Dto;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using WeixinManagerBase = Senparc.Xncf.WeixinManagerBase;
using Senparc.Weixin.MP;

namespace Senparc.Xncf.WeixinManagerMP
{
    [XncfOrder(4100)]
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.WeixinManagerMP";

        public override string Uid => "122A0B1C-27E7-4670-BBEB-687AD75CF874";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "微信公众号管理模块";

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
            WeixinManagerMPSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as WeixinManagerMPSenparcEntities;

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
            services.AddSession();
              services.AddScoped<Senparc.Xncf.WeixinManagerBase.Domain.Services.UserService>();
          services.AddScoped<WeixinManagerBase.OHS.Local.AppService.WeixinRegisterService>();
            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var weixinRegisterService = scope.ServiceProvider.GetService<WeixinManagerBase.OHS.Local.AppService.WeixinRegisterService>();

                weixinRegisterService.RegisterWeixin((r, s) =>
                {
                    r.RegisterMpAccount(s, "NCF 微信公众号");
                    return 0;
                }).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            app.UseSession();

            return base.UseXncfModule(app, registerService);

        }
    }
}
