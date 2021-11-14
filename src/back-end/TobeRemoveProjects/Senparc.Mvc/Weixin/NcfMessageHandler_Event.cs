using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Log;
using Senparc.NeuChar.Entities;
using Senparc.Service;
using Senparc.Weixin.MP.Entities;
using System;

namespace Senparc.Mvc.Weixin
{
    public partial class NcfMessageHandler
    {
        public override IResponseMessageBase OnEvent_ScanRequest(RequestMessageEvent_Scan requestMessage)
        {
            try
            {
                if (int.TryParse(requestMessage.EventKey, out int sceneId))
                {
                    var serviceProvider = SenparcDI.GetServiceProvider();

                    //临时二维码
                    var qrCodeRegCache = serviceProvider.GetService<QrCodeRegCache>();
                    var qrCodeRegData = qrCodeRegCache.Get(sceneId.ToString());

                    if (qrCodeRegData?.Data != null && qrCodeRegData.Data.Ticket == requestMessage.Ticket)
                    {
                        var responseRegMessage = CreateResponseMessage<ResponseMessageText>();
                        if (qrCodeRegData.Data.QrCodeRegDataType == QrCodeRegDataType.Reg)
                        {
                            var userService = serviceProvider.GetService<AccountService>();
                            //判断是否已经绑定[CodeChecked]
                            if (userService.GetCount(z => z.WeixinOpenId == requestMessage.FromUserName) <= 0)
                            {
                                var code = new Random().Next(100000, 999999).ToString();
                                qrCodeRegData.Data.Code = code;
                                qrCodeRegData.Data.OpenId = requestMessage.FromUserName;
                                qrCodeRegCache.Remove(sceneId.ToString());
                                qrCodeRegCache.Insert(qrCodeRegData.Data, sceneId.ToString());
                                responseRegMessage.Content =
                                    "验证码：" + code;
                            }
                            else
                            {
                                responseRegMessage.Content = "您的微信已绑定NCF账户，不能重复绑定！";
                            }
                            return responseRegMessage;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtility.Weixin.Error(ex.Message, ex);
            }
            return base.OnEvent_ScanRequest(requestMessage);
        }
    }
}