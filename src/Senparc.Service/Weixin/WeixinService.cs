using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Cache;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models.WeixinTemplate;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.Containers;
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Service
{
    public class WeixinService /*: IWeixinService*/
    {

        private readonly IServiceProvider _serviceProvider;

        public WeixinService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string ReplaceWeixinFace(string content)
        {
            var weixinFaceCahce = _serviceProvider.GetService<WeixinFaceCache>();
            foreach (var keyValuePair in weixinFaceCahce.Data)
            {
                var image = $"<img src=\"/Content/WeixinFace/{keyValuePair.Value}.png\" title=\"{keyValuePair.Value.ToString()}\" />";
                //表情中有<>之类符号，需要考虑到进来的content已经HtmlEncode的情况
                var encodedCode = keyValuePair.Key.HtmlEncode();
                if (encodedCode != keyValuePair.Key)
                {
                    content = content.Replace(keyValuePair.Key.HtmlEncode(), image);
                }
                content = content.Replace(keyValuePair.Key, image);
            }
            return content;
        }

        public string GetStandardKeyword(string keyword, int maxKeywordCount = 0)
        {
            //整理keywords格式
            if (!keyword.IsNullOrEmpty())
            {
                var keywords = keyword.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (maxKeywordCount > 0)
                {
                    keywords = keywords.Take(maxKeywordCount).ToList();
                }

                return $"|{string.Join("|", keywords)}|";
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="filePath">相对路径</param>
        /// <returns></returns>
        public bool DownloadTemplate(string serverId, string filePath)
        {
            try
            {
                //创建目录
                LogUtility.Weixin.Debug($"DownloadTemplate:path {filePath} serverId={serverId}");
                var downloadTemplateImage = DownloadTemplate(serverId, filePath, false);
                LogUtility.Weixin.Debug("DownloadTemplate:downloadTemplateImage " + downloadTemplateImage);
                if (!downloadTemplateImage)
                {
                    downloadTemplateImage = DownloadTemplate(serverId, filePath, true);
                }

                if (!downloadTemplateImage)
                {
                    throw new Exception("图片上传错误（01）！");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtility.Weixin.Debug("DownloadTemplate exception: " + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// 下载微信临时素材Image
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="fileName"></param>
        /// <param name="getNewToken"></param>
        /// <returns></returns>
        public bool DownloadTemplate(string serverId, string fileName, bool getNewToken = false)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var senparcWeixinSetting = _serviceProvider.GetService<IOptions<SenparcWeixinSetting>>().Value;
                MediaApi.Get(senparcWeixinSetting.WeixinAppId, serverId, ms);
                //保存到文件
                ms.Position = 0;
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                //判断是否上传成功
                byte[] topBuffer = new byte[1];
                ms.Read(topBuffer, 0, 1);
                if (topBuffer[0] == '{')
                {
                    //写入日志
                    ms.Position = 0;
                    byte[] logBuffer = new byte[1024];
                    ms.Read(logBuffer, 0, logBuffer.Length);
                    string str = System.Text.Encoding.Default.GetString(logBuffer);
                    Senparc.Ncf.Log.LogUtility.Weixin.InfoFormat("下载失败：{0}。serverId：{1}", str, serverId);
                    return false;
                }
                ms.Position = 0;
                //创建目录
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                    }
                    fs.Flush();
                }
                Senparc.Ncf.Log.LogUtility.Weixin.InfoFormat("下载成功：Path[{0}]", fileName);
            }
            return true;
        }


        public SendTemplateMessageResult SendTemplateMessage(IWeixinTemplateBase data, string openId, string url = null)
        {
            try
            {
                var dt1 = DateTime.Now;
                //TODO:写到配置文件里
                var accessToken = AccessTokenContainer.TryGetAccessToken(
                    "wxbe855a981c34aa3f", "2d65ad33a2e8d03c79ecd7b035522227", true);
                var dt2 = DateTime.Now;

                var result = TemplateApi.SendTemplateMessage(accessToken, openId, data.TemplateId, url, data);
                var dt3 = DateTime.Now;

                LogUtility.Weixin.InfoFormat("发送模板信息【{2}】。获取AccessToken时间：{0}ms，发送时间：{1}ms",
                    (dt2 - dt1).TotalMilliseconds, (dt3 - dt2).TotalMilliseconds, data.TemplateName);
                return result;
            }
            catch (Exception ex)
            {
                LogUtility.Weixin.ErrorFormat($"发送模板消息出错【{data.TemplateName}】：{ex.Message}", ex);
            }
            return null;
        }
        public OAuthUserInfo GetOAuthResult(string appId, string appSecret, string code)
        {
            OAuthAccessTokenResult result = null;

            result = OAuthApi.GetAccessToken(appId, appSecret, code);

            if (result.errcode != (int)ReturnCode.请求成功)
            {
                throw new Exception(result.errcode + result.errmsg);
            }

            return OAuthApi.GetUserInfo(result.access_token, result.openid);
        }
    }
}