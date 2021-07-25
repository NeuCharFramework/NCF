using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Repository;
using Senparc.Ncf.SMS;
using Senparc.Ncf.XncfBase;
using Senparc.Respository;
using Senparc.Weixin;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Senparc.Web
{
    /// <summary>
    /// 全局注册
    /// </summary>
    public static class Register
    {
        public static void AddNcfServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env, CompatibilityVersion compatibilityVersion)
        {
            //如果运行在IIS中，需要添加IIS配置
            //https://docs.microsoft.com/zh-cn/aspnet/core/host-and-deploy/iis/index?view=aspnetcore-2.1&tabs=aspnetcore2x#supported-operating-systems
            //services.Configure<IISOptions>(options =>
            //{
            //    options.ForwardClientCertificate = false;
            //});

            //启用以下代码强制使用 https 访问
            //services.AddHttpsRedirection(options =>
            //{
            //    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            //    options.HttpsPort = 443;
            //});

            //读取Log配置文件
            var repository = LogManager.CreateRepository("NETCoreRepository");
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));

            //提供网站根目录
            if (env.ContentRootPath != null)
            {
                SiteConfig.ApplicationPath = env.ContentRootPath;
                SiteConfig.WebRootPath = env.WebRootPath;
            }

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //services.AddMvc(options =>
            //{
            //    //options.Filters.Add<HttpGlobalExceptionFilter>();
            //})
            //.SetCompatibilityVersion(compatibilityVersion)
            //.AddRazorPagesOptions(options =>
            //{
            //    //options.AllowAreas = true;//支持 Area
            //    //options.AllowMappingHeadRequestsToGetHandler = false;//https://www.learnrazorpages.com/razor-pages/handler-methods
            //})


            services.AddMultiTenant();//注册多租户（按需）
            EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntitiesMultiTenant));//注册多租户数据库的对象（按需）
            services.AddScoped<ITenantInfoDbData,TenantInfoDbData>();
            services.AddScoped<TenantInfoRepository>();
            services.AddScoped<IClientRepositoryBase<TenantInfo>,TenantInfoRepository>();

            services.AddSenparcGlobalServices(configuration);//注册 CO2NET 基础引擎所需服务

            var builder = services.AddRazorPages(opt =>
                {
                    //opt.RootDirectory = "/";
                })
              .AddNcfAreas(env)//注册所有 Ncf 的 Area 模块（必须）
              .ConfigureApiBehaviorOptions(options =>
              {
                  options.InvalidModelStateResponseFactory = actionContext =>
                  {
                      var values = actionContext.ModelState.Where(_ => _.Value.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid).Select(_ => new { _.Key, Errors = _.Value.Errors.Select(__ => __.ErrorMessage) });
                      AjaxReturnModel commonReturnModel = new AjaxReturnModel<object>(values);
                      commonReturnModel.Success = false;
                      commonReturnModel.Msg = "参数校验未通过";
                      //commonReturnModel.StatusCode = Core.App.CommonReturnStatusCode.参数校验不通过;
                      return new BadRequestObjectResult(commonReturnModel);
                  };
              })
              .AddXmlSerializerFormatters()
              .AddJsonOptions(options =>
              {
                  //忽略循环引用
                  //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                  //不使用驼峰样式的key
                  //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  //设置时间格式
                  //options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
              })
              //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-2.1&tabs=aspnetcore2x
              //.AddSessionStateTempDataProvider()
              //忽略JSON序列化过程中的循环引用：https://stackoverflow.com/questions/7397207/json-net-error-self-referencing-loop-detected-for-type
              .AddRazorPagesOptions(options =>
              {
                  //自动注册  防止跨站请求伪造（XSRF/CSRF）攻击
                  options.Conventions.Add(new Core.Conventions.AutoValidateAntiForgeryTokenModelConvention());
              });

#if DEBUG
            //Razor启用运行时编译，多个项目不需要手动编译。
            if (env.IsDevelopment())
            {
                builder.AddRazorRuntimeCompilation(options =>
                {
                    //自动索引所有需要使用 RazorRuntimeCompilation 的模块
                    foreach (var razorRegister in XncfRegisterManager.RegisterList.Where(z => z is IXncfRazorRuntimeCompilation))
                    {
                        try
                        {
                            var libraryPath = ((IXncfRazorRuntimeCompilation)razorRegister).LibraryPath;
                            options.FileProviders.Add(new PhysicalFileProvider(libraryPath));
                        }
                        catch (Exception ex)
                        {
                            SenparcTrace.BaseExceptionLog(ex);
                        }
                    }
                });
            }

#endif

            //TODO:在模块中注册
            services.AddScoped<Ncf.AreaBase.Admin.Filters.AuthenticationResultFilterAttribute>();

            //支持 Session
            services.AddSession();
            //解决中文进行编码问题
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            //使用内存缓存
            services.AddMemoryCache();

            //注册 SignalR
            services.AddSignalR();
            //注册 Lazy<T>
            services.AddTransient(typeof(Lazy<>));

            services.Configure<SenparcCoreSetting>(configuration.GetSection("SenparcCoreSetting"));
            services.Configure<SenparcSmsSetting>(configuration.GetSection("SenparcSmsSetting"));

            //自动依赖注入扫描
            services.ScanAssamblesForAutoDI();
            //已经添加完所有程序集自动扫描的委托，立即执行扫描（必须）
            AssembleScanHelper.RunScan();
            //services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();

            ////添加多租户
            //services.AddMultiTenant();

            //注册 User 登录策略
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserAnonymous", policy =>
                {
                    policy.RequireClaim("UserMember");
                });
            });
            services.AddHttpContextAccessor();

            //Repository & Service
            services.AddScoped<ISysButtonRespository,SysButtonRespository>();

            //Other
            services.AddScoped<IAdminWorkContextProvider,AdminWorkContextProvider>();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();

            //忽略某些 API
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Work.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_MiniProgram.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_Open.ToString());
            //Senparc.CO2NET.WebApi.Register.OmitCategoryList.Add(Senparc.NeuChar.PlatformType.WeChat_OfficialAccount.ToString());

            //激活 Xncf 扩展引擎（必须）
            services.StartEngine(configuration);
        }

        public static void UseNcf(this IApplicationBuilder app, IWebHostEnvironment env,
            IOptions<SenparcCoreSetting> senparcCoreSetting,
            IOptions<SenparcSetting> senparcSetting)
        {
            Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting = senparcCoreSetting.Value;

            // 启动 CO2NET 全局注册，必须！
            // 关于 UseSenparcGlobal() 的更多用法见 CO2NET Demo：https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
            var registerService = app
                //全局注册
                .UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
                {
                    #region CO2NET 全局配置

                    #region 全局缓存配置（按需）

                    #region 配置和使用 Redis

                    //配置全局使用Redis缓存（按需，独立）
                    if (UseRedis(senparcSetting.Value, out string redisConfigurationStr))//这里为了方便不同环境的开发者进行配置，做成了判断的方式，实际开发环境一般是确定的，这里的if条件可以忽略
                    {
                        /* 说明：
                         * 1、Redis 的连接字符串信息会从 Config.SenparcSetting.Cache_Redis_Configuration 自动获取并注册，如不需要修改，下方方法可以忽略
                        /* 2、如需手动修改，可以通过下方 SetConfigurationOption 方法手动设置 Redis 链接信息（仅修改配置，不立即启用）
                         */
                        Senparc.CO2NET.Cache.CsRedis.Register.SetConfigurationOption(redisConfigurationStr);

                        //以下会立即将全局缓存设置为 Redis
                        Senparc.CO2NET.Cache.CsRedis.Register.UseKeyValueRedisNow(); //键值对缓存策略（推荐）
                        /*Senparc.CO2NET.Cache.CsRedis.Register.UseHashRedisNow();*/ //HashSet储存格式的缓存策略 

                        //也可以通过以下方式自定义当前需要启用的缓存策略
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisObjectCacheStrategy.Instance);//键值对
                        //CacheStrategyFactory.RegisterObjectCacheStrategy(() => RedisHashSetObjectCacheStrategy.Instance);//HashSet
                    }
                    //如果这里不进行Redis缓存启用，则目前还是默认使用内存缓存 

                    #endregion

                    #endregion

                    #region 注册日志（按需，建议）

                    globalRegister.RegisterTraceLog(ConfigTraceLog); //配置TraceLog

                    #endregion


                    #endregion
                });

            //XncfModules（必须）
            Senparc.Ncf.XncfBase.Register.UseXncfModules(app, registerService);
            //TODO:app.UseXncfModules(registerService);

            #region .NET Core默认不支持GB2312

            //http://www.mamicode.com/info-detail-2225481.html
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            #endregion

            #region Senparc.Core 设置

            //用于解决HttpContext.Connection.RemoteIpAddress为null的问题
            //https://stackoverflow.com/questions/35441521/remoteipaddress-is-always-null
            app.UseHttpMethodOverride(new HttpMethodOverrideOptions
            {
                //FormFieldName = "X-Http-Method-Override"//此为默认值
            });

            #endregion

            #region 异步线程

            {
                ////APM Ending 数据统计
                //var utility = new APMNeuralDataThreadUtility();
                //Thread thread = new Thread(utility.Run) { Name = "APMNeuralDataThread" };
                //SiteConfig.AsynThread.Add(thread.Name, thread);
            }

            SiteConfig.AsynThread.Values.ToList().ForEach(z =>
            {
                z.IsBackground = true;
                z.Start();
            }); //全部运行 

            //更多 XNCF 模块线程已经集成到 Senparc.Ncf.XncfBase.Register.ThreadCollection 中

            #endregion

        }


        /// <summary>
        /// 判断当前配置是否满足使用 Redis（根据是否已经修改了默认配置字符串判断）
        /// </summary>
        /// <param name="senparcSetting"></param>
        /// <returns></returns>
        internal static bool UseRedis(SenparcSetting senparcSetting, out string redisConfigurationStr)
        {
            redisConfigurationStr = senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*默认值，不启用*/;
            return useRedis;
        }


        /// <summary>
        /// 配置微信跟踪日志
        /// </summary>
        private static void ConfigTraceLog()
        {
            //这里设为Debug状态时，/App_Data/WeixinTraceLog/目录下会生成日志文件记录所有的API请求日志，正式发布版本建议关闭

            //如果全局的IsDebug（Senparc.CO2NET.Config.IsDebug）为false，此处可以单独设置true，否则自动为true
            Senparc.CO2NET.Trace.SenparcTrace.SendCustomLog("系统日志",
                "NeuCharFramework 系统启动"); //只在Senparc.Weixin.Config.IsDebug = true的情况下生效

            //全局自定义日志记录回调
            Senparc.CO2NET.Trace.SenparcTrace.OnLogFunc = () =>
            {
                //加入每次触发Log后需要执行的代码
            };

            //当发生基于WeixinException的异常时触发
            WeixinTrace.OnWeixinExceptionFunc = ex =>
            {
                //加入每次触发WeixinExceptionLog后需要执行的代码

                //发送模板消息给管理员
                //var eventService = new Senparc.Weixin.MP.Sample.CommonService.EventService();
                //eventService.ConfigOnWeixinExceptionFunc(ex);
            };
        }

    }
}
