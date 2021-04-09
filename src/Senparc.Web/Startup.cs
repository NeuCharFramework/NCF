using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MySql;//根据需要添加
using Senparc.Ncf.Database.Sqlite;//根据需要添加
using Senparc.Ncf.Database.SqlServer;//根据需要添加
using Senparc.Ncf.Service.MultiTenant;
using Senparc.Web.Hubs;

namespace Senparc.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            this.env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //指定数据库类型
            //services.AddDatabase<SqliteMemoryDatabaseConfiguration>();//使用 SQLite 数据库
            services.AddDatabase<SQLServerDatabaseConfiguration>();//使用 SQLServer数据库
            //services.AddDatabase<MySqlDatabaseConfiguration>();//使用 MySQL 数据库

            //添加（注册） Ncf 服务（重要，必须！）
            services.AddNcfServices(Configuration, env, CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IOptions<SenparcCoreSetting> senparcCoreSetting,
            IOptions<SenparcSetting> senparcSetting,
            IHubContext<ReloadPageHub> hubContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }


            #region 多租户

            app.UseMiddleware<TenantMiddleware>();//如果不启用多租户功能，可以删除此配置

            #endregion

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCookiePolicy();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            //Use NCF（必须）
            app.UseNcf(env, senparcCoreSetting, senparcSetting);
        }
    }
}