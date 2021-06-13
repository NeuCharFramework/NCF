using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.XncfBase;
using Senparc.Weixin;
using Senparc.Weixin.Cache.CsRedis;
using Senparc.Weixin.Entities;
using Senparc.Weixin.RegisterServices;
using System;
using System.Collections.Generic;

namespace MyExtensionCode
{

    public class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口
        public override bool IgnoreInstall => true;//禁止出现安装提示

        public override string Name => "自定义代码";

        public override string Uid => "FFD88E78-9069-465E-A533-C52E60AEB3FE";

        public override string Version => "";

        public override string MenuName => "";

        public override string Icon => "";

        public override string Description =>
            @"此项目为扩展代码示例项目，不会因为 NCF 框架更新而受影响。
              如果您需要扩展代码，请参考此项目新建项目。本项目请在发布到生产环境之前移除！";

        public override IList<Type> Functions => new Type[] { };

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSenparcWeixinServices(configuration);
            return base.AddXncfModule(services, configuration);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                //从 appsettings.json 获取微信原始注册信息
                var senparcSetting = scope.ServiceProvider.GetService<IOptions<SenparcSetting>>();
                var senparcWeixinSetting = scope.ServiceProvider.GetService<IOptions<SenparcWeixinSetting>>();
                //注册 微信
                registerService //使用 Senparc.Weixin SDK
                    .UseSenparcWeixin(senparcWeixinSetting.Value, weixinRegister =>
                    {
                        #region Weixin 设置

                        /* 微信配置开始
                         * 
                         * 建议按照以下顺序进行注册，尤其须将缓存放在第一位！
                         */

                        //注册开始

                        #region 微信缓存（按需，必须在 register.UseSenparcWeixin () 之前）

                        //微信的 Redis 缓存，如果不使用则注释掉（开启前必须保证配置有效，否则会抛错）
                        if (UseRedis(senparcSetting.Value, out string redisConfigurationStr))//这里为了方便不同环境的开发者进行配置，做成了判断的方式，实际开发环境一般是确定的，这里的if条件可以忽略
                        {
                            weixinRegister.UseSenparcWeixinCacheCsRedis();
                        }

                        #endregion

                        #region 注册公众号或小程序（按需）

                        ////注册公众号（可注册多个）
                        //weixinRegister.RegisterMpAccount(senparcWeixinSetting.Value, "NCF")
                        //                  .RegisterMpAccount("AppId", "Secret", "Senparc_Template")

                        #endregion
                            ;

                        #endregion

                    });
            }

            //基类中会自动注册所有已经添加到数据库的公众号
            return base.UseXncfModule(app, registerService);
        }

        #endregion


        /// <summary>
        /// 判断当前配置是否满足使用 Redis（根据是否已经修改了默认配置字符串判断）
        /// </summary>
        /// <param name="senparcSetting"></param>
        /// <returns></returns>
        internal bool UseRedis(SenparcSetting senparcSetting, out string redisConfigurationStr)
        {
            redisConfigurationStr = senparcSetting.Cache_Redis_Configuration;
            var useRedis = !string.IsNullOrEmpty(redisConfigurationStr) && redisConfigurationStr != "#{Cache_Redis_Configuration}#"/*默认值，不启用*/;
            return useRedis;
        }

    }
}
