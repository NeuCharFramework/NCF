using System;
using System.Linq;
using System.Threading;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Email;
using Senparc.Ncf.Core.Enums;

namespace Senparc.Threads
{
    using Senparc.Ncf.Core.Cache;
    using Senparc.Ncf.Core.Extensions;
    using Senparc.Ncf.Log;

    /// <summary>
    /// 自动发送邮件类
    /// </summary>
    public class AutoSendEmailThreadUtility
    {
        public static bool OpenAutoSendEmail = true;//设置自动发送状态
        /// <summary>
        /// 开启状态（自动转换，不要手动设置）
        /// </summary>
        public static bool AutoSendEmailStarted { get; private set; }
        public static int AutoSendCount = 0;//尝试发送次数
        public static int SendSuccessCount = 0;//成功发送次数
        /// <summary>
        /// 下一次发送时间
        /// </summary>
        public static DateTime NextSendTime { get; set; }


        /// <summary>
        /// 设置休眠
        /// </summary>
        /// <param name="seconds"></param>
        private void SetSleep(int seconds)
        {
            NextSendTime = DateTime.Now.AddSeconds(seconds);
            Thread.Sleep(seconds * 1000);
        }

        public void SendEventHandler(object state, bool timeout)
        {
            try
            {
                //LogUtility.EmailLogger.Info("开始Email发送轮询");
                if (!OpenAutoSendEmail)
                {
                    AutoSendEmailStarted = false;
                    SetSleep(10);
                    return; //continue;
                }
                AutoSendEmailStarted = true;


                //HttpContext cont = new HttpContext(new HttpRequest("Default.aspx", "http://www.senparc.com/Default.aspx", ""), new HttpResponse(null));

                SendEmailCache emailCache = new SendEmailCache();
                SendEmail sendEmail = new SendEmail(null);
                var emails = emailCache.Data.ToList();//这里添加ToList为了防止Data中途有变化，抛出异常：System.InvalidOperationException: 集合已修改；可能无法执行枚举操作。
                foreach (var email in emails)
                {
                    if (!OpenAutoSendEmail)
                    {
                        break;
                    }

                    AutoSendCount++;
                    if (email.SendCount > SiteConfig.MaxSendEmailTimes)
                    {
                        continue;
                    }
                    bool useBackEmail = email.SendCount >= 3;//当3次发送失败后，使用备用邮箱
                    var accountType = useBackEmail ? EmailAccountType._163 : EmailAccountType.Default;

                    //if (accountType == EmailUserType.Default && email.SendCount == 5 && email.Address.Contains("@163"))
                    //{
                    //    accountType = EmailUserType._163;
                    //}

                    if (email.SendCount > 2)
                    {
                        accountType = EmailAccountType._163;
                    }
                    else if (email.SendCount > 1)
                    {
                        accountType = EmailAccountType.Souidea;
                    }

                    if (sendEmail.DoSendEmail(email.Address, email.Subject, email.Body, email.UserName, false, accountType, true))
                    {
                        emailCache.SendSuccess(email);
                        SendSuccessCount++;
                        LogUtility.EmailLogger.Info($"缓存自动发送Email成功（by {accountType}）：{email.Address}，Subject:{email.Subject}");
                    }
                    else
                    {
                        emailCache.SendFail(email);
                        LogUtility.EmailLogger.Error($"缓存自动发送Email失败（by {accountType}）：{email.Address}，Subject:{email.Subject}");
                    }
                    SetSleep(7);
                }
                if (emailCache.Data.Count == 0)
                {
                    SetSleep(5);
                }
            }
            catch (Exception ex)
            {
                LogUtility.EmailLogger.ErrorFormat("AutoSendEmail发送错误！", ex);
            }
        }

        public void Send()
        {
            SetSleep(20);
            do
            {
                SendEventHandler(null, false);
            } while (true);
        }

    }
}
