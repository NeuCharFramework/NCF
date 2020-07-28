using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.AspNet.HttpUtility;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.Utilities;
using Senparc.Core.Utility;
using Senparc.Mvc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MvcExtension;
using System;
using System.IO;

namespace Senparc.Mvc.Controllers
{
    public class WeixinController : Controller
    {
        private readonly SenparcWeixinSetting _senparcWeixinSetting;
        private readonly Func<string> _getRandomFileName = () => DateTime.Now.ToString("yyyyMMdd-HHmmss") + Guid.NewGuid().ToString("n").Substring(0, 6);
        public WeixinController(IOptions<SenparcWeixinSetting> options)
        {
            _senparcWeixinSetting = options.Value;
        }
        [HttpGet]
        [ActionName("Index")]
        public ActionResult Get(string signature, string timestamp, string nonce, string echostr)
        {
            if (CheckSignature.Check(signature, timestamp, nonce, _senparcWeixinSetting.Token))
            {
                return Content(echostr); //返回随机字符串则表示验证通过
            }
            else
            {
                return Content("failed:" + signature + "," + CheckSignature.GetSignature(timestamp, nonce, _senparcWeixinSetting.Token));
            }
        }

        /// <summary>
        /// 用户发送消息后，微信平台自动Post一个请求到这里，并等待响应XML。
        /// PS：此方法为简化方法，效果与OldPost一致。
        /// v0.8之后的版本可以结合Senparc.Weixin.MP.MvcExtension扩展包，使用WeixinResult，见MiniPost方法。
        /// </summary>
        [HttpPost]
        [ActionName("Index")]
        public ActionResult Post(PostModel postModel)
        {
            if (!CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, _senparcWeixinSetting.Token))
            {
                return Content("参数错误！");
            }

            #region 打包 PostModel 信息

            postModel.Token = _senparcWeixinSetting.Token;//根据自己后台的设置保持一致
            postModel.EncodingAESKey = "";//根据自己后台的设置保持一致
            postModel.AppId = _senparcWeixinSetting.WeixinAppId;//根据自己后台的设置保持一致

            #endregion

            //v4.2.2之后的版本，可以设置每个人上下文消息储存的最大数量，防止内存占用过多，如果该参数小于等于0，则不限制
            var maxRecordCount = 10;

            //自定义MessageHandler，对微信请求的详细判断操作都在这里面。
            var messageHandler = new NcfMessageHandler(Request.GetRequestMemoryStream(), postModel, maxRecordCount);

            #region 设置消息去重

            /* 如果需要添加消息去重功能，只需打开OmitRepeatedMessage功能，SDK会自动处理。
             * 收到重复消息通常是因为微信服务器没有及时收到响应，会持续发送2-5条不等的相同内容的RequestMessage*/
            messageHandler.OmitRepeatedMessage = true;//默认已经开启，此处仅作为演示，也可以设置为false在本次请求中停用此功能

            #endregion

            try
            {
                #region 记录 Request 日志

                var logPath = ServerUtility.ContentRootMapPath(string.Format("~/logs/mp/{0}/", DateTime.Now.ToString("yyyy-MM-dd")));
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }

                //测试时可开启此记录，帮助跟踪数据，使用前请确保App_Data文件夹存在，且有读写权限。
                messageHandler.RequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_{1}_{2}.txt", _getRandomFileName(),
                    messageHandler.RequestMessage.FromUserName,
                    messageHandler.RequestMessage.MsgType)));
                if (messageHandler.UsingEncryptMessage)
                {
                    messageHandler.EcryptRequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_Ecrypt_{1}_{2}.txt", _getRandomFileName(),
                        messageHandler.RequestMessage.FromUserName,
                        messageHandler.RequestMessage.MsgType)));
                }

                #endregion

                //执行微信处理过程
                messageHandler.Execute();

                #region 记录 Response 日志

                //测试时可开启，帮助跟踪数据

                //if (messageHandler.ResponseDocument == null)
                //{
                //    throw new Exception(messageHandler.RequestDocument.ToString());
                //}
                if (messageHandler.ResponseDocument != null)
                {
                    messageHandler.ResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_{1}_{2}.txt", _getRandomFileName(),
                        messageHandler.ResponseMessage.ToUserName,
                        messageHandler.ResponseMessage.MsgType)));
                }

                if (messageHandler.UsingEncryptMessage && messageHandler.FinalResponseDocument != null)
                {
                    //记录加密后的响应信息
                    messageHandler.FinalResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_Final_{1}_{2}.txt", _getRandomFileName(),
                        messageHandler.ResponseMessage.ToUserName,
                        messageHandler.ResponseMessage.MsgType)));
                }

                #endregion

                //return Content(messageHandler.ResponseDocument.ToString());//v0.7-
                //return new WeixinResult(messageHandler);//v0.8+
                return new FixWeixinBugWeixinResult(messageHandler);//为了解决官方微信5.0软件换行bug暂时添加的方法，平时用下面一个方法即可
            }
            catch (Exception ex)
            {
                #region 异常处理
                SenparcTrace.Log($"MessageHandler错误：{ex.Message}");

                using (TextWriter tw = new StreamWriter(ServerUtility.ContentRootMapPath("~/App_Data/Error_" + _getRandomFileName() + ".txt")))
                {
                    tw.WriteLine("ExecptionMessage:" + ex.Message);
                    tw.WriteLine(ex.Source);
                    tw.WriteLine(ex.StackTrace);
                    //tw.WriteLine("InnerExecptionMessage:" + ex.InnerException.Message);

                    if (messageHandler.ResponseDocument != null)
                    {
                        tw.WriteLine(messageHandler.ResponseDocument.ToString());
                    }

                    if (ex.InnerException != null)
                    {
                        tw.WriteLine("========= InnerException =========");
                        tw.WriteLine(ex.InnerException.Message);
                        tw.WriteLine(ex.InnerException.Source);
                        tw.WriteLine(ex.InnerException.StackTrace);
                    }

                    tw.Flush();
                    tw.Close();
                }
                return Content("");
                #endregion
            }
        }
    }
}
