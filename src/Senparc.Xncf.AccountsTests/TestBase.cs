using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Accounts.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Api;
using Senparc.Ncf.Database;
using Senparc.Xncf.SystemCore.Domain.Database;
using Senparc.Xncf.Accounts.Models;
using Senparc.Xncf.Accounts.Domain.Models;

namespace Senparc.Xncf.AccountsTests
{
    public class TestBase
    {
        internal IServiceCollection ServiceCollection { get; set; }
        internal IServiceProvider ServiceProvider { get; set; }

        public TestBase()
        {
            var args = new string[0];
            var webBuilder = WebApplication.CreateBuilder(args);
            ServiceCollection = new ServiceCollection();


            ServiceCollection.AddScoped<AccountOperationLog>();
            ServiceCollection.AddScoped<AccountOperationLogService>();


            //激活 Xncf 扩展引擎（必须）
            var logMsg = webBuilder.StartWebEngine<SQLServerDatabaseConfiguration>();
            //如果不需要启用 Areas，可以只使用 services.StartEngine() 或 services.StartEngine<TDatabaseConfiguration>() 方法

            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");


            //

            #region 覆盖数据库上下文实体
            //Func<IServiceProvider, SenparcEntities> senparcEntitiesImplementationFactory = s =>
            //{
            //    var multipleDatabasePool = MultipleDatabasePool.Instance;

            //    return multipleDatabasePool.GetXncfDbContext(this.GetType(), serviceProvider: s) as SenparcEntities;
            //};

            //ServiceCollection.AddScoped<SenparcEntitiesDbContextBase>(senparcEntitiesImplementationFactory);// 继承自 DbContext
            //ServiceCollection.AddScoped<ISenparcEntitiesDbContext>(senparcEntitiesImplementationFactory);
            //ServiceCollection.AddScoped<SenparcEntitiesBase>(senparcEntitiesImplementationFactory);//继承自 SenparcEntitiesMultiTenantBase
            //ServiceCollection.AddScoped<SenparcEntities>(senparcEntitiesImplementationFactory);
            #endregion

            var app = webBuilder.Build();
            ServiceProvider = ServiceCollection.BuildServiceProvider();

            IWebHostEnvironment env = app.Environment;
            IOptions<SenparcSetting> senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();
            IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();

            // 启动 CO2NET 全局注册，必须！
            // 关于 UseSenparcGlobal() 的更多用法见 CO2NET Demo：https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
            var registerService = app
                //全局注册
                .UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
                {
                   
                });


            //XncfModules（必须）
            app.UseXncfModules(registerService);
        }
    }
}
