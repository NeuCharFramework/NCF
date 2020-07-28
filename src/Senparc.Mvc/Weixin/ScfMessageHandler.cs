using Senparc.NeuChar.Entities;
using Senparc.NeuChar.Entities.Request;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using System.IO;

namespace Senparc.Mvc.Weixin
{
    /// <summary>
    /// 
    /// </summary>
    public partial class NcfMessageHandler : MessageHandler<NcfMessageContext>
    {
        public NcfMessageHandler(Stream stream, PostModel postModel, int maxRecordCount)
            : base(stream, postModel, maxRecordCount)
        {
        }

        public override void OnExecuting()
        {

        }

        public override IResponseMessageBase OnTextRequest(RequestMessageText requestMessage)
        {
            var defaultResponseMessage = base.CreateResponseMessage<ResponseMessageText>();
            var requestHandler =
                requestMessage.StartHandler()
                    //关键字不区分大小写，按照顺序匹配成功后将不再运行下面的逻辑
                    .Keyword("code", () =>
                    {
                        defaultResponseMessage.Content =
                            @"code";
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
