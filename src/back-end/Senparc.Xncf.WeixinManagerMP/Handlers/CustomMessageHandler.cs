using Microsoft.Extensions.DependencyInjection;
using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.Entities.Request;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Xncf.WeixinManagerBase.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerMP.Handlers
{
    public class CustomMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        UserService _userService;
        IServiceScope _scope;

        public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
            _scope = serviceProvider.CreateScope();
            _userService = _scope.ServiceProvider.GetService<UserService>();
        }

        ~CustomMessageHandler()
        {
            _scope.Dispose();
        }

        public override async Task<IResponseMessageBase> OnTextOrEventRequestAsync(RequestMessageText requestMessage)
        {
            var requestHandler = await requestMessage.StartHandler()
                .Keyword("1", () =>
                {
                    var responseMessageFor1 = base.CreateResponseMessage<ResponseMessageText>();

                    var user = _userService.GetObjectAsync(z => z.MpOpenId == OpenId).Result;

                    responseMessageFor1.Content = "你输入了1\r\n你的OpenID：" + user.MpOpenId + "\r\n你的ID：" + user.Id;
                    return responseMessageFor1;
                })
                .Keywords(new[] { "0", "你好" }, () =>
                {
                    var responseMessageForHello = base.CreateResponseMessage<ResponseMessageText>();
                    responseMessageForHello.Content = "你也好！";
                    return responseMessageForHello;
                })
                .Regex(@"^1\d{10}$", () =>
                {
                    var responseMessageForPhone = base.CreateResponseMessage<ResponseMessageText>();
                    responseMessageForPhone.Content = "你输入了手机号：" + requestMessage.Content;
                    return responseMessageForPhone;
                }).Default(async () =>
                {
                    var responseMessageForDefault = base.CreateResponseMessage<ResponseMessageText>();
                    responseMessageForDefault.Content = @$"您发了一条文本消息，内容是：{requestMessage.Content}";
                    return responseMessageForDefault;
                });

            return requestHandler.GetResponseMessage();
        }

        public override async Task<IResponseMessageBase> OnImageRequestAsync(RequestMessageImage requestMessage)
        {
            await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(Senparc.Weixin.Config.SenparcWeixinSetting.MpSetting.WeixinAppId, base.OpenId, "您发了一张图片：");

            var responseMessage = base.CreateResponseMessage<ResponseMessageImage>();
            responseMessage.Image = new Image()
            {
                MediaId = requestMessage.MediaId
            };
            return responseMessage;
        }


        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = @$"这条消息来自DefaultMpMessageContext
您发送的消息类型为：{requestMessage.MsgType.ToString()}";

            return responseMessage;
        }

        public override async Task OnExecutingAsync(CancellationToken cancellationToken)
        {
            var user = await _userService.GetObjectAsync(z => z.MpOpenId == OpenId);
            if (user == null)
            {
                user = await _userService.CreateUserAsync(null, null, mpOpenId: OpenId);
            }

            await _userService.UpdateLastUpdateTime(user);

            await base.OnExecutingAsync(cancellationToken);
        }
    }
}
