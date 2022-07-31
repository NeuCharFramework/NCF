using Microsoft.AspNetCore.Builder;
using Senparc.Ncf.XncfBase;
using Senparc.Weixin.MP.MessageHandlers.Middleware;
using Senparc.Xncf.WeixinManagerMP.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerMP
{
    public partial class Register : IXncfMiddleware
    {
        public IApplicationBuilder UseMiddleware(IApplicationBuilder app)
        {
            //Stream, PostModel, int, IServiceProvider

            //使用公众号的 MessageHandler 中间件（不再需要创建 Controller）
            app.UseMessageHandlerForMp("/WeixinMessage", (stream, postModel, maxRecord, sp) => new CustomMessageHandler(stream, postModel, maxRecord, serviceProvider: sp), options =>
            {
                options.AccountSettingFunc = context => Senparc.Weixin.Config.SenparcWeixinSetting;
            });

            return app;
        }
    }
}
