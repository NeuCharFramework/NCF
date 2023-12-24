using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.CO2NET.MessageQueue;
using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.MessageHandlers;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Xncf.WeixinManager;
using Senparc.Xncf.WeixinManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Web
{
    [MpMessageHandlerAttribute("JeffreyMp")]
    public class JeffreyMpMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        private readonly MpAccountDto _mpAccountDto;
        private readonly ServiceProvider _services;


        //特殊的构造函数
        public JeffreyMpMessageHandler(MpAccountDto mpAccountDto, Stream stream, PostModel postModel, int maxRecordCount, ServiceProvider services) : this(stream, postModel, maxRecordCount)
        {
            this._mpAccountDto = mpAccountDto;
            this._services = services;
        }

        public JeffreyMpMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
        }
        //public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        //{
        //    var requestContent = requestMessage.Content;
        //    var responseMessage = this.CreateResponseMessage<ResponseMessageText>();

        //    responseMessage.Content = "已收到信息，请稍等";//requestContent.Replace("?", "!").Replace("？", "！").Replace("吗", "");

        //    #region 发送到 AI

        //    //定义 AI 请求参数
        //    var parameter = new PromptConfigParameter()
        //    {
        //        MaxTokens = 2000,
        //        Temperature = 0.7,
        //        TopP = 0.5,
        //    };

        //    //定义 AI 模型
        //    var modelName = "gpt-35-turbo";

        //    //获取 AI 处理器
        //    SemanticAiHandler _semanticAiHandler = (SemanticAiHandler)base.ServiceProvider.GetRequiredService<IAiHandler>();

        //    //配置和初始化模型
        //    var chatConfig = _semanticAiHandler.ChatConfig(parameter, userId: "Jeffrey",
        //        modelName: modelName /*, modelName: "gpt-4-32k"*/);

        //    var iWantToRun = chatConfig.iWantToRun;

        //    //发送到 AI 模型，获取结果
        //    var result = await _semanticAiHandler.ChatAsync(iWantToRun, requestContent);

        //    //异步发送 AI 结果到用户
        //    _ = Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, result.Output);

        //    #endregion

        //    return responseMessage;
        //}


        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            var reponseMessage = this.CreateResponseMessage<ResponseMessageText>();
            //reponseMessage.Content =  requestMessage.Content.Replace("?","!").Replace("？","！").Replace("吗","");

            var services = base.ServiceProvider;

            var smq = new SenparcMessageQueue();
            smq.Add("WechatBot:" + requestMessage.MsgId,
                async () =>
                {
                    //定义 AI 请求参数
                    var parameter = new PromptConfigParameter()
                    {
                        MaxTokens = 2000,
                        Temperature = 0.7,
                        TopP = 0.5
                    };

                    //定义 AI 模型
                    var modelName = "gpt-35-turbo";//text-davinci-003

                    //获取 AI 处理器
                    SemanticAiHandler _semanticAiHandler = (SemanticAiHandler)services.GetRequiredService<IAiHandler>();

                    //配置和初始化模型
                    var chatConfig = _semanticAiHandler.ChatConfig(parameter, userId: "Jeffrey",
                                                   modelName: modelName /*, modelName: "gpt-4-32k"*/);

                    var iWantToRun = chatConfig.iWantToRun;

                    //发送到 AI 模型，获取结果
                    var result = await _semanticAiHandler.ChatAsync(iWantToRun, requestMessage.Content);

                    //异步发送 AI 结果到用户
                    _ = Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, result.Output);
                });

            reponseMessage.Content = "消息已收到，正在思考中……";
            return reponseMessage;
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "欢迎关注 Jeffrey Su 的公众号，现在时间：" + SystemTime.Now;
            return responseMessage;
        }
    }
}
