using Senparc.NeuChar.Entities;
using Senparc.NeuChar.Entities.Request;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Xncf.WeixinManager;
using System.IO;
using System.Threading.Tasks;

namespace MyExtensionCode
{
    /// <summary>
    /// 自定义公众号消息处理器
    /// </summary>
    [MpMessageHandler("MpMessageHandlerForTest")]
    public partial class MpAccountMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        public MpAccountMessageHandler(Stream stream, PostModel postModel, int maxRecordCount)
            : base(stream, postModel, maxRecordCount)
        {
        }

        public override void OnExecuting()
        {

        }

        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            var defaultResponseMessage = base.CreateResponseMessage<ResponseMessageText>();
            var requestHandler =
                requestMessage.StartHandler()
                    //关键字不区分大小写，按照顺序匹配成功后将不再运行下面的逻辑
                    .Keyword("code", () =>
                    {
                        defaultResponseMessage.Content = @"code";
                        return defaultResponseMessage;
                    }).Keyword("ncf",()=> {
                        var register = new Register();
                        defaultResponseMessage.Content = register.Description;
                        return defaultResponseMessage;
                    }).Default(() =>
                    {
                        defaultResponseMessage.Content = @"你发送了文字：" + requestMessage.Content;
                        return defaultResponseMessage;
                    });

            return requestHandler.GetResponseMessage() as IResponseMessageBase;
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = @"感谢您关注！";
            return responseMessage;
        }
    }
}
