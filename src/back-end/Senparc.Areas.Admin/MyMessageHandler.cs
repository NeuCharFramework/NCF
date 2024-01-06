using Microsoft.Extensions.DependencyInjection;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Xncf.WeixinManager;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Web
{
    [MpMessageHandler("JeffreyMp")]
    public class MyMessageHandler : XncfMpMessageHandler<WechatAiContext>
    {
        public MyMessageHandler(MpAccountDto mpAccountDto, Stream stream, PostModel postModel, int maxRecordCount, ServiceProvider services) : base(mpAccountDto, stream, postModel, maxRecordCount, services)
        {
        }

        public override async Task<IResponseMessageBase> OnVoiceRequestAsync(RequestMessageVoice requestMessage)
        {
            var content = requestMessage.Recognition;
            var textRequestMessage = new RequestMessageText()
            {
                FromUserName = requestMessage.FromUserName,
                ToUserName = requestMessage.ToUserName,
                CreateTime = requestMessage.CreateTime,
                Content = content,
                MsgId = requestMessage.MsgId
            };
            return await OnTextRequestAsync(textRequestMessage);
        }
    }
}
