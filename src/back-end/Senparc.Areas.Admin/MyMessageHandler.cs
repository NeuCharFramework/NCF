using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.TextToImage;
using Senparc.AI;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Xncf.WeixinManager;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
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
            var mediaId = requestMessage.MediaId;

            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = $"您刚才发送了语音：{content}。MediaId：{mediaId}";

            return responseMessage;
        }

        protected override async Task AfterRunBotAsync(IServiceProvider serviceProvider, RequestMessageText requestMessage, MpAccountDto mpAccountDto, SenparcAiResult senparcAiResult, DateTimeOffset startTime)
        {
            var aiResultContent = senparcAiResult.OutputString;
            if (aiResultContent == "Img=True")
            {
                _ = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, $"我开始画画啦！");

                var dalleSetting = ((SenparcAiSetting)Senparc.AI.Config.SenparcAiSetting)["AzureDallE3"];

                //绘制图片，并返回
                var userId = "Jeffrey";
                var semanticAiHandler = serviceProvider.GetService<SemanticAiHandler>();
                var iWantTo = semanticAiHandler.IWantTo(dalleSetting)
                                    .ConfigModel(ConfigModel.ImageGeneration, userId)
                                    .BuildKernel();

#pragma warning disable SKEXP0002 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
#pragma warning disable SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                var dallE = iWantTo.GetRequiredService<ITextToImageService>();


                var imageUrl = await dallE.GenerateImageAsync(requestMessage.Content, 1024, 1024);
#pragma warning restore SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。

                _ = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, $"图片已生成，正在保存并推送（{imageUrl}）");

                //开始保存图片
                var filePath = Senparc.CO2NET.Utilities.ServerUtility.ContentRootMapPath($"~/Senparc.AI.Dalle-{SystemTime.NowTicks}.jpg");

                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    await Senparc.CO2NET.HttpUtility.Get.DownloadAsync(serviceProvider, imageUrl, fs);
                    await fs.FlushAsync();

                    fs.Close();
                    await Console.Out.WriteLineAsync("图片已保存：" + filePath);
                }

                try
                {
                    //保存微信图片素材
                    var uploadResult = await Senparc.Weixin.MP.AdvancedAPIs.MediaApi.UploadTemporaryMediaAsync(mpAccountDto.AppId, Senparc.Weixin.MP.UploadMediaFileType.image, filePath, timeOut: 50000000);

                    //推送图片
                    await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendImageAsync(mpAccountDto.AppId, requestMessage.FromUserName, uploadResult.media_id);
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.Message);
                    await Console.Out.WriteLineAsync(ex.StackTrace?.ToString());
                    await Console.Out.WriteLineAsync(ex.InnerException?.Message);

                    _ = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, $"图片生成失败：" + ex.Message);

                }
#pragma warning restore SKEXP0002 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            }
            else
            {
                await base.AfterRunBotAsync(serviceProvider, requestMessage, mpAccountDto, senparcAiResult, startTime);
            }
        }
    }
}
