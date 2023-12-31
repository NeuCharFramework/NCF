using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.MessageQueue;
using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.MessageHandlers;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Xncf.WeixinManager;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using SenparcConf.Xncf.WechatBot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Web
{
    [MpMessageHandlerAttribute("JeffreyMp")]
    public class JeffreyMpMessageHandler : MessageHandler<WechatAiContext> //MessageHandler<DefaultMpMessageContext>
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
            this.GlobalMessageContext.MaxRecordCount = 10;
            this.GlobalMessageContext.ExpireMinutes = 2;
        }

        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
          
            //reponseMessage.Content =  requestMessage.Content.Replace("?","!").Replace("？","！").Replace("吗","");

            var services = base.ServiceProvider;

            var smq = new SenparcMessageQueue();
            var smqKey = "WechatBot:" + requestMessage.MsgId;
            smq.Add(smqKey,
                async () =>
                {
                    var dt = SystemTime.Now;

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
                    var iWantToRun = WechatAiContext.GetIWantToRun(services, parameter, modelName, requestMessage.FromUserName);
                    SemanticAiHandler semanticAiHandler = iWantToRun.SemanticAiHandler;

                    //发送到 AI 模型，获取结果
                    var result = await semanticAiHandler.ChatAsync(iWantToRun, requestMessage.Content);

                    if (result==null)
                    {
                        await Console.Out.WriteLineAsync("result is null");
                    }

                    //异步发送 AI 结果到用户
                    _ = Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, result.Output);

                    _ = Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, $"总共耗时：{SystemTime.DiffTotalMS(dt)}ms");
                });

            var reponseMessage = this.CreateResponseMessage<ResponseMessageText>();
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
