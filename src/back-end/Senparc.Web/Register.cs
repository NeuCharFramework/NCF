﻿using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.WebApi;
using Senparc.CO2NET.WebApi.WebApiEngines;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.AreasBase;

namespace Senparc.Web
{
    /// <summary>
    /// 全局注册
    /// </summary>
    public static class Register
    {
        private static System.DateTime StartTime = SystemTime.Now.DateTime;

        public static void AddNcf(this WebApplicationBuilder builder)
        {
            StartTime = SystemTime.Now.DateTime;

            //激活 Xncf 扩展引擎（必须）
            var logMsg = builder.StartWebEngine(new[] { "Senparc.Areas.Admin" });
            //如果不需要启用 Areas，可以只使用 services.StartEngine() 或 services.StartEngine() 方法

            Console.WriteLine("============ logMsg =============");
            Console.WriteLine(logMsg);
            Console.WriteLine("============ logMsg END =============");


            #region 仅在完全删除 Senparc.Xncf.Swagger 时启用以下代码！

            // 如果项目中不引用 Senparc.Xncf.Swagger，需要使用下方代码手动启用 DynamicAPI。更多示例参考：
            // https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.net7/Startup.cs

            //var services = builder.Services;
            //var mvcBuilder = services.AddMvcCore();
            //services.AddAndInitDynamicApi(mvcBuilder, options =>
            //{
            //    options.DefaultRequestMethod = ApiRequestMethod.Get;
            //    options.BaseApiControllerType = null;
            //    options.CopyCustomAttributes = true;
            //    options.TaskCount = Environment.ProcessorCount * 10;
            //    options.ShowDetailApiLog = true;
            //    options.AdditionalAttributeFunc = null;
            //    options.ForbiddenExternalAccess = false;
            //    options.UseLowerCaseApiName = true;
            //});

            #endregion


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
        }

        public static void UseNcf<TDatabaseConfiguration>(this WebApplication app)
            where TDatabaseConfiguration : IDatabaseConfiguration, new()
        {
            //注入DI对象
            app.UseSenparcMvcDI();

            IWebHostEnvironment env = app.Environment;
            IOptions<SenparcSetting> senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();
            IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();

            // 启动 CO2NET 全局注册，必须！
            // 关于 UseSenparcGlobal() 的更多用法见 CO2NET Demo：https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
            var registerService = app
                //全局注册
                .UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
                {
                    //配置全局使用Redis缓存（按需，独立）
                    SetCache(senparcSetting);

                    #region 注册日志（按需，建议）

                    globalRegister.RegisterTraceLog(ConfigTraceLog); //配置TraceLog

                    #endregion
                });

            //XncfModules（必须）
            app.UseXncfModules(registerService)
               .UseNcfDatabase<TDatabaseConfiguration>();

            /*  UseNcfDatabase<TDatabaseConfiguration>() 泛型类型说明
             *                
             *                  方法                            |         说明
             * -------------------------------------------------|-------------------------
             *  UseNcf<BySettingDatabaseConfiguration>()        |  由 appsettings.json 决定配置
             *  UseNcf<SqlServerDatabaseConfiguration>()        |  使用 SQLServer 数据库
             *  UseNcf<SqliteMemoryDatabaseConfiguration>()     |  使用 SQLite 数据库
             *  UseNcf<MySqlDatabaseConfiguration>()            |  使用 MySQL 数据库
             *  UseNcf<PostgreSQLDatabaseConfiguration>()       |  使用 PostgreSQL 数据库
             *  UseNcf<OracleDatabaseConfiguration>()           |  使用 Oracle 数据库（V12+）
             *  UseNcf<OracleDatabaseConfigurationForV11>()     |  使用 Oracle 数据库（V11+）
             *  UseNcf<DmDatabaseConfiguration>()               |  使用 达梦 数据库
             *  更多数据库可扩展，依次类推……
             *  
             */
        }

        /// <summary>
        /// 配置全局使用Redis缓存（按需，独立）
        /// </summary>
        /// <param name="senparcSetting"></param>
        private static void SetCache(IOptions<SenparcSetting> senparcSetting)
        {
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
        }

        /// <summary>
        /// 输出启动成功标志
        /// </summary>
        /// <param name="app"></param>
        public static void ShowSuccessTip(this WebApplication app)
        {
            //输出启动成功标志
            Senparc.Ncf.Core.VersionManager.ShowSuccessTip($"\t\t启动工作准备就绪\r\n\t\t用时：{SystemTime.NowDiff(StartTime).TotalSeconds} s");
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
        }

    }
}
