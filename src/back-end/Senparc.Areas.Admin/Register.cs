/* 
 * 特别注意：
 * 当前注册类是比较特殊的底层系统支持模块，
 * 其中加入了一系列特殊处理的代码，并不适合所有模块使用，
 * 如果需要学习扩展模块，请参考 【Senparc.ExtensionAreaTemplate】 项目的 Register.cs 文件！
 */

using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.Areas.Admin.ACL.Repository;
//using Senparc.Areas.Admin.Authorization;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin
{
    [XncfRegister]
    [XncfOrder(5996)]
    public class Register : XncfRegisterBase,
        IXncfRegister, //注册 XNCF 基础模块接口（必须）
        IAreaRegister, //注册 XNCF 页面接口（按需选用）
        IXncfDatabase  //注册 XNCF 模块数据库（按需选用）
                       //IXncfRazorRuntimeCompilation  //需要使用 RazorRuntimeCompilation，在开发环境下实时更新 Razor Page
    {

        #region IXncfRegister 接口

        public override string Name => "NeuCharFramework.Admin";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_AREAS_ADMIN_UID;// "00000000-0000-0001-0001-000000000001";

        public override string Version => "0.3.0-beta4";

        public override string MenuName => "NCF 系统管理员后台";

        public override string Icon => "fa fa-university";

        public override string Description => "这是管理员后台模块，用于 NCF 系统后台的自我管理，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";


        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
            //var adminModule = xncfModuleServiceExtension.GetObject(z => z.Uid == this.Uid);
            //if (adminModule == null)
            //{
            //    //只在未安装的情况下进行安装，InstallModuleAsync会访问到此方法，不做判断可能会引发死循环。
            //    //常规模块中请勿在此方法中自动安装模块！
            //    await xncfModuleServiceExtension.InstallModuleAsync(this.Uid).ConfigureAwait(false);
            //}

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO：应该提供一个 BeforeUninstall 方法，阻止卸载。


            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AdminSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AdminSenparcEntities;

            //指定需要删除的数据实体

            //注意：这里作为演示，在卸载模块的时候删除了所有本模块创建的表，实际操作过程中，请谨慎操作，并且按照删除顺序对实体进行排序！
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }


        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //Attributes
            services.AddScoped<AuthenticationResultFilterAttribute>();
            //services.AddScoped(typeof(AuthenticationAsyncPageFilterAttribute));

            services.Configure<JwtSettings>(JwtSettings.Position_Backend, configuration.GetSection(JwtSettings.Position_Backend));// 配置管理后台jwt

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //AutoMap映射
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<AdminUserInfo, CreateOrUpdate_AdminUserInfoDto>();
            });

            AddJwtAuthentication(services, configuration);


            return base.AddXncfModule(services, configuration, env);
        }


        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });

            return base.UseXncfModule(app, registerService);
        }

        #endregion

        #region IAreaRegister 接口

        public string HomeUrl => "/Admin";

        public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>()
        {
            new AreaPageMenuItem(GetAreaUrl("/Admin/Menu/Index"),"菜单管理","fa fa-bug"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/SenparcTrace/Index"),"SenparcTrace 日志","fa fa-calendar-o"),
        };//Admin比较特殊，不需要全部输出


        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            Console.WriteLine("areaRegisterTypes: AuthorizeConfig - AdminArea");

            //鉴权配置
            //添加基于Cookie的权限验证：https://docs.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-2.1&tabs=aspnetcore2x
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(AdminAuthorizeAttribute.AuthenticationScheme, options =>
                {
                    options.AccessDeniedPath = "/Admin/Forbidden/";
                    options.LoginPath = "/Admin/Login/";
                    options.Cookie.HttpOnly = false;
                });

            builder.Services
                //.AddAuthorization(options =>
                .AddAuthorizationCore(options =>
                {
                    options.AddPolicy("AdminOnly", policy =>
                    {
                        policy.RequireClaim("AdminMember");
                    });
                });

            builder.AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizePage("/", "AdminOnly");//必须登录
                options.Conventions.AllowAnonymousToPage("/Login");//允许匿名

                //更多：https://docs.microsoft.com/en-us/aspnet/core/security/authorization/razor-pages-authorization?view=aspnetcore-2.2
            });

            SenparcTrace.SendCustomLog("系统启动", "完成 Area:Admin 注册");
            builder.Services.AddScoped<ISysMenuRepository, SysMenuRepository>();
            builder.Services.AddScoped<ISysRolePermissionRepository, SysRolePermissionRepository>();
            builder.Services.AddScoped<IAuthorizationHandler, Ncf.Core.Authorization.PermissionHandler>();

            return builder;
        }

        #endregion

        #region IXncfDatabase 接口

        public const string DATABASE_PREFIX = "ADMIN_";
//NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//系统表，将会留空
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);


        public void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //services.AddScoped<AdminUserInfo>();
        }

        //public string DatabaseUniquePrefix => "NcfSystemAdmin_";
        //public Type XncfDatabaseDbContextType => typeof(SenparcEntities);


        //public void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    //已在 SenparcEntities 中完成

        //}

        //public void AddXncfDatabaseModule(IServiceCollection services)
        //{
        //    #region 历史解决方案参考信息
        //    /* 参考信息
        //     *      错误信息：
        //     *          中文：EnableRetryOnFailure 解决短暂的数据库连接失败
        //     *          英文：Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
        //     *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
        //     *      问题解决方案说明：https://www.colabug.com/2329124.html
        //     */

        //    /* 参考信息
        //     *      错误信息：
        //     *          中文：EnableRetryOnFailure 解决短暂的数据库连接失败
        //     *          英文：Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
        //     *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
        //     *      问题解决方案说明：https://www.colabug.com/2329124.html
        //     */
        //    #endregion

        //    Func<IServiceProvider, SenparcEntities> implementationFactory = s =>
        //        new SenparcEntities(new DbContextOptionsBuilder<SenparcEntities>()
        //            .UseSqlServer(Ncf.Core.Config.SenparcDatabaseConfigs.ClientConnectionString,
        //                            b => base.DbContextOptionsAction(b, "Senparc.Web"))
        //            .Options);

        //    services.AddScoped(implementationFactory);
        //    services.AddScoped<ISenparcEntities>(implementationFactory);
        //    services.AddScoped<SenparcEntitiesBase>(implementationFactory);

        //    services.AddScoped(typeof(INcfClientDbData), typeof(NcfClientDbData));
        //    services.AddScoped(typeof(INcfDbData), typeof(NcfClientDbData));

        //    //Attributes
        //    services.AddScoped(typeof(AuthenticationResultFilterAttribute));
        //    services.AddScoped(typeof(AuthenticationAsyncPageFilterAttribute));

        //    //预加载 EntitySetKey
        //    EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntities));

        //    //AutoMap映射
        //    base.AddAutoMapMapping(profile =>
        //    {
        //        profile.CreateMap<AdminUserInfo, CreateOrUpdate_AdminUserInfoDto>();
        //    });
        //}


        #endregion

        //#region IXncfRazorRuntimeCompilation 接口
        //public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Areas.Admin"));
        //#endregion

        /// <summary>
        /// 添加前后端认证
        /// </summary>
        /// <param name="services"></param>
        private void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            JwtSettings backend = new JwtSettings();
            configuration.Bind(JwtSettings.Position_Backend, backend);
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
        public override void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration)
        {
            base.OnAutoMapMapping(services, configuration);
            services.AddAutoMapper(z => z.AddProfile<AutoMpperProfiles.SenparcAreaAdminAutoMapperProfile>());
        }
    }

}
