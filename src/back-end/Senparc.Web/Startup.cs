using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
//using Senparc.Ncf.Database.MySql;//根据需要添加或删除，使用需要引用包： Senparc.Ncf.Database.MySql
//using Senparc.Ncf.Database.Sqlite;//根据需要添加或删除，使用需要引用包： Senparc.Ncf.Database.Sqlite
using Senparc.Ncf.Database.SqlServer;//根据需要添加或删除，使用需要引用包： Senparc.Ncf.Database.SqlServer
using Senparc.Ncf.Service.MultiTenant;
using Senparc.Web.Hubs;
using System.Collections.Generic;
using System;
using Senparc.Core;
using Senparc.Ncf.Database.PostgreSQL;

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
            /* AddDatabase<TDatabaseConfiguration>() 泛型类型说明：
             * 
             *                  方法                            |         说明
             * -------------------------------------------------|-------------------------
             *  AddDatabase<SQLServerDatabaseConfiguration>()   |  使用 SQLServer 数据库
             *  AddDatabase<SqliteMemoryDatabaseConfiguration>()|  使用 SQLite 数据库
             *  AddDatabase<MySqlDatabaseConfiguration>()       |  使用 MySQL 数据库
             *  AddDatabase<PostgreSQLDatabaseConfiguration>()  |  使用 PostgreSQL 数据库
             *  更多数据库可扩展，依次类推……
             *  
             */
            services.AddDatabase<SQLServerDatabaseConfiguration>();//默认使用 SQLServer数据库，根据需要改写

            //添加（注册） Ncf 服务（重要，必须！）
            services.AddNcfServices(Configuration, env);
            services.Configure<Core.JwtSettings>(Core.JwtSettings.Position_Backend, Configuration.GetSection(Core.JwtSettings.Position_Backend));// 配置管理后台jwt
            services.Configure<Core.JwtSettings>(Core.JwtSettings.Position_MiniPro, Configuration.GetSection(Core.JwtSettings.Position_MiniPro));// 配置前台jwt
            addJwtAuthentication(services);
        }

        /// <summary>
        /// 添加前后端认证
        /// </summary>
        /// <param name="services"></param>
        private void addJwtAuthentication(IServiceCollection services)
        {
            JwtSettings backend = new JwtSettings();
            JwtSettings miniPro = new JwtSettings();
            Configuration.Bind(JwtSettings.Position_Backend, backend);
            Configuration.Bind(JwtSettings.Position_MiniPro, miniPro);
            services.AddAuthentication()
                .AddJwtBearer(BackendJwtAuthorizeAttribute.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidIssuer = backend.Issuer,
                        ValidAudience = backend.Audience,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(backend.SecretKey)),
                        ValidateIssuer = true, //whether or not valid Issuer
                        ValidateAudience = true, //whether or not valid Audience
                        ValidateLifetime = true, //whether or not valid out-of-service time
                        ValidateIssuerSigningKey = true, //whether or not valid SecurityKey　　　　　　　　　　　
                        ClockSkew = System.TimeSpan.Zero//Allowed server time offset
                    };
                })
            //.AddJwtBearer(Core.ApiAttributes.JwtAuthorizeAttribute.AuthenticationScheme, options =>
            //{
            //    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
            //    {
            //        ValidIssuer = miniPro.Issuer,
            //        ValidAudience = miniPro.Audience,
            //        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(miniPro.SecretKey)),
            //        ValidateIssuer = true, //whether or not valid Issuer
            //        ValidateAudience = true, //whether or not valid Audience
            //        ValidateLifetime = true, //whether or not valid out-of-service time
            //        ValidateIssuerSigningKey = true, //whether or not valid SecurityKey　　　　　　　　　　　
            //        ClockSkew = System.TimeSpan.Zero//Allowed server time offset
            //    };
            //})
            ;

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
                endpoints.MapControllers();
            });

            //Use NCF（必须）
            app.UseNcf(env, senparcCoreSetting, senparcSetting);
        }
    }
}
