using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Resources;

using Senparc.Xncf.Accounts.Models;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.Accounts
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly ResourceManager ResourceManager = new("Senparc.Xncf.Accounts.AccountsResource", typeof(AccountsResource).Assembly);

        private static string T(string key, string fallback)
        {
            return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
        }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Accounts";

        public override string Uid => Senparc.Ncf.Core.Config.SiteConfig.SYSTEM_XNCF_MODULE_ACCOUNTS_UID;//不可修改

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => T("Accounts.Register.MenuName", "用户管理");

        public override string Icon => "fa fa-users";

        public override string Description => T("Accounts.Register.Description", "注册用户管理");

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
            AccountSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AccountSenparcEntities;

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
            //注册 User 登录策略
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserAnonymous", policy =>
                {
                    policy.RequireClaim("UserMember");
                });
            });

            return base.AddXncfModule(services, configuration, env);
        }
    }
}
