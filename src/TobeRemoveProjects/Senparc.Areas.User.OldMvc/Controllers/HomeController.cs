using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Senparc.Areas.User.Models.VD;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.HttpUtility;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Log;
using Senparc.Mvc;
using Senparc.Mvc.Filter;
using Senparc.Service;
using Senparc.Ncf.Utility;
using Senparc.Weixin.Entities;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.QrCode;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Senparc.Ncf.Service;
using Senparc.Ncf.SMS;

namespace Senparc.Areas.User.Controllers
{
    [MenuFilter("Home")]
    public class HomeController : BaseUserController
    {
        private readonly AccountService _accountService;
        private readonly SmsRecordService _smsRecordService;
        private readonly SenparcWeixinSetting _senparcWeixinSetting;

        public HomeController(AccountService accountService, SmsRecordService smsRecordService, IOptionsSnapshot<SenparcWeixinSetting> senparcWeixinSetting)
        {
            _accountService = accountService;
            _smsRecordService = smsRecordService;
            _senparcWeixinSetting = senparcWeixinSetting.Value;
        }
       
        public ActionResult Index()
        {
            Home_IndexVD vd = new Home_IndexVD()
            {
            };
            return View(vd);
        }

        public ActionResult Recover()
        {
            Home_RecoverVD vd = new Home_RecoverVD()
            {
            };
            return View(vd);
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            int lastSeconds = -1;
            if (Request.HttpContext.Session.GetString("LastSendSmsTime") != null)
            {
                var lastSendSmsTime = DateTime.Parse(Request.HttpContext.Session.GetString("LastSendSmsTime"));
                var secondsSpan = lastSendSmsTime.AddSeconds(SiteConfig.SMSSENDWAITSECONDS) - DateTime.Now;
                lastSeconds = secondsSpan.TotalSeconds > 0 ? (int)secondsSpan.TotalSeconds : -1;
            }

            var regGuid = Guid.NewGuid();
            Request.HttpContext.Session.SetString("RegGuid", regGuid.ToString("N"));
            var vd = new Home_RegisterVD()
            {
                LastSeconds = lastSeconds,
                SmsToken = _smsRecordService.SetSendSmsToken(),
                RegGuid = regGuid,
                QrUrl = GetRegQrCode(regGuid)
            };

            return View(vd);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Register(Home_RegisterVD vd_form)
        {
            //判断是否已经绑定微信
            var codeRegCache = SenparcDI.GetService<QrCodeRegCache>();
            var messageItem = codeRegCache.MessageQueue.FirstOrDefault(z => z.Data.RegGuid == vd_form.RegGuid);
            if (messageItem != null)
            {
                //允许注册
                var qrCodeRegData = messageItem.Data;
                var user = _accountService.GetObject(z => z.WeixinOpenId == qrCodeRegData.OpenId);
                if (user != null)
                {
                    return RedirectToAction("Login", "Home");
                }
            }
            else
            {
                //未发送验证码
                return RedirectToAction("Register", "Home");
            }

            var userNameForbiddenWords = new[] { "@", "~", "`", "\"", "/", "<", ">", "'", "*", " ", "　" };
            this.Validator(vd_form.UserName, "用户名", "UserName", false)
                .IsFalse(z => userNameForbiddenWords.Any(z.Contains), "{0}不能包含空格及特殊字符！", true)
                .MinLength(4)
                .IsFalse(z => Regex.IsMatch(z, @"^\d+$"), "不能全是数字", true)
                .IsFalse(z => _accountService.CheckUserNameExisted(0, z), "{0}已存在！", true);

            this.Validator(vd_form.Password, "密码", "Password")
                .IsNotNull(vd_form.Password, "请填写密码！", true)
                .MinLength(6)
                .IsEqual(vd_form.RePassword, "两次密码不一致", true);
            this.Validator(vd_form.Email, "Email", "Email", false)
                .IsEmail(true)
                .IsFalse(z => _accountService.CheckEmailExisted(0, z), "{0}已存在！", true);
            this.Validator(vd_form.Phone, "手机号", "Phone", false).IsMobile(true)
                .IsFalse(z => _accountService.CheckPhoneExisted(0, z), "{0}已存在！", true);
            var phoneCheckVal = this.Validator(vd_form.PhoneCheck, "验证码", "PhoneCheck", false);
            var qrCodeCheckVal = this.Validator(vd_form.QrCodeCheck, "微信验证码", "QrCodeCheck", false);

            if (ModelState.IsValid)
            {
                if (!SiteConfig.IsDebug)
                {
                    //短信验证
                    var phoneCheckCodeCache = SenparcDI.GetService<PhoneCheckCodeCache>();
                    var data = phoneCheckCodeCache.Get(vd_form.PhoneCheck);
                    phoneCheckVal.IsTrue(z => data != null && data.Data.Phone == vd_form.Phone, "{0}错误！", true);
                    qrCodeCheckVal.IsTrue(z => messageItem.Data != null && messageItem.Data.Code == vd_form.QrCodeCheck, "{0}错误！", true);
                }
            }

            if (!ModelState.IsValid)
            {
                var smsRecord = SenparcDI.GetService<SmsRecordService>();
                var smsToken = smsRecord.GetSendSmsToken();//获取已经保存的令牌
                vd_form.SmsToken = smsToken;
                return View(vd_form);
            }
            try
            {
                _accountService.CreateAccount(vd_form.UserName.Trim(), vd_form.Email.Trim(), vd_form.Phone.Trim(), vd_form.Password.Trim(),
                     messageItem.Data.OpenId, "", "");
                codeRegCache.Remove(messageItem.Data.Key);//删除缓存记录
                SetMessager(MessageType.success, "注册成功");
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                SetMessager(MessageType.danger, "注册失败，请稍后重试");
                var smsRecord = SenparcDI.GetService<SmsRecordService>();
                var smsToken = smsRecord.GetSendSmsToken();//获取已经保存的令牌
                vd_form.SmsToken = smsToken;
                return View(vd_form);
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        //[AllowAnonymous]
        //public ActionResult Login(string returnUrl)
        //{
        //    if (IsLogined)
        //    {
        //        if (returnUrl.IsNullOrEmpty())
        //        {
        //            return RedirectToAction("Index", "Home");
        //        }
        //        return Redirect(returnUrl.UrlDecode());
        //    }
        //    var vd = new Home_LoginVD()
        //    {
        //        ReturnUrl = returnUrl
        //    };
        //    int.TryParse(Request.HttpContext.Session.GetString("TryLoginTimes"), out var tryLoginTimes);
        //    vd.ShowCheckCode = tryLoginTimes > SiteConfig.TryUserLoginTimes;
        //    return View(vd);
        //}

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(base.UserName) && base.IsLogined)
            {
                return RedirectToAction("Index");
            }

            //未登录
            var remoteDomain = "https://weixin.senparc.com"; //SiteConfig.IsDebug ? "https://localhost:44305" : "https://weixin.senparc.com";
            var loaclDomain = $"{Request.Scheme}://{Request.Host.Value}"; //SiteConfig.IsDebug ? "http://localhost:11942" : "https://scf.senparc.com";

            var callbackUrl = $"{loaclDomain}/User/Home/OAuthCallback?returnUrl={returnUrl.UrlEncode()}";

            var url = $"{remoteDomain}/OAuth2/Authorize?appId={_senparcWeixinSetting.WeixinAppId}&redirect_uri={callbackUrl.UrlEncode()}&response_type=code&state=NCF&scope=snsapi_base#senparc_redirect";
            return Redirect(url);
        }

        /// <summary>
        /// 向weixin.senparc.com请求的OAuth回调
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="returnUrl"></param>
        /// <param name="weixinId"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult OAuthCallback(string code, string state, string returnUrl, int weixinId = 18635)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return Content("您拒绝了授权，请返回。");
            }

            if (state != "NCF")
            {
                return Content("验证失败！请从正规途径进入！");
            }

            try
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    (s, certificate, chain, sslPolicyErrors) => true;

                var url = "https://weixin.senparc.com/APIs/User_Info";

                var userInfoJson = RequestUtility.HttpPost(url,
                    formData: new Dictionary<string, string>
                    {
                        {"code", code},
                        {"appId", _senparcWeixinSetting.WeixinAppId},
                        {"appSecret", _senparcWeixinSetting.WeixinAppSecret}
                    }
                );
                LogUtility.Account.Debug($"UserInfoJson:{userInfoJson}");
                var userInfo = JsonConvert.DeserializeObject<UserInfoJson>(userInfoJson);

                if (userInfo == null || userInfo.openid == null)
                {
                    //获取出错
                    var errorResult = JsonConvert.DeserializeObject<WxJsonResult>(userInfoJson);
                    return RenderError($"错误：{errorResult.errcode.ToString()}");
                }

                var account = _accountService.CreateOrUpdateBySenparcWeixin(userInfo);

                _accountService.Login(account.UserName, true, null, true);

                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    return RedirectToAction(nameof(Index), "Home", new { area = "User" });
                }
                //转到用户一开始访问的页面
                return Redirect(returnUrl);
            }
            catch (ErrorJsonResultException ex)
            {
                return Content(ex.Message);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(Home_LoginVD vd)
        {
            if (IsLogined)
            {
                return RedirectToAction("Index", "Home");
            }

            this.Validator(vd.UserName, "用户名或Email", "UserName", false);
            this.Validator(vd.Password, "密码", "Password").IsNotNull(vd.Password, "请填写密码！", true);

            //string errorMsg = null;
            //if (ModelState.IsValid && Session["TryLoginTimes"] != null)
            //{
            //    //验证码
            //    var tryLoginTimes = (int)Session["TryLoginTimes"];
            //    if (SiteConfig.TryUserLoginTimes <= tryLoginTimes)
            //    {
            //        var valCheckCode = this.Validator(vd.CheckCode, "验证码", "CheckCode", null)
            //                               .ValidateCheckCode(CheckCodeKind.Login, true);
            //        if (valCheckCode == null)
            //        {
            //            errorMsg = "验证码错误！";
            //        }
            //    }
            //}

            if (ModelState.IsValid)
            {
                var user = _accountService.TryLogin(vd.UserName, vd.Password, false, true);
                this.Validator(user, "账号", "UserName")
                    .IsNotNull(user, "账号或密码错误！", true);
            }

            if (!ModelState.IsValid)
            {
                int.TryParse(Request.HttpContext.Session.GetString("TryLoginTimes"), out var tryLoginTimes);

                vd.ShowCheckCode = tryLoginTimes >= SiteConfig.TryUserLoginTimes;

                Request.HttpContext.Session.SetString("TryLoginTimes", (tryLoginTimes + 1).ToString());
                return View(vd);
            }
            Request.HttpContext.Session.Remove("TryLoginTimes");
            if (vd.ReturnUrl.IsNullOrEmpty())
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(vd.ReturnUrl.UrlDecode());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult SendSms(string phone, string token)
        {
            //手机号是否存在
            bool exist = _accountService.CheckPhoneExisted(0, phone);
            if (exist)
            {
                return RenderJsonSuccessResult(false, new { Message = "手机号码已存在！" });
            }
            var session = Request.HttpContext.Session;
            //发送时间间隔
            if (session.GetString("LastSendSmsTime") != null)
            {
                var lastSeconds = 60 - (DateTime.Now - DateTime.Parse(session.GetString("LastSendSmsTime"))).TotalSeconds;
                if (lastSeconds > 0)
                {
                    return RenderJsonSuccessResult(false, new { Message = $"短信已发送，请查收，或在{(int)lastSeconds}秒后重试！" });
                }
            }

            var smsResult = _smsRecordService.SendPhoneCheck(Request.HttpContext.Connection.RemoteIpAddress.ToString(), phone, token);
            LogUtility.SmsLogger.InfoFormat("发送验证短信，号码：{0}，IP：{1}，Token：{2}", phone, Request.HttpContext.Connection.RemoteIpAddress.ToString(), _smsRecordService.GetSendSmsToken());

            if (smsResult == SmsResult.成功)
            {
                session.SetString("LastSendSmsTime", DateTime.Now.ToString());
                return RenderJsonSuccessResult(true, new { Message = "短信发送成功，请查收短信！", LastSendSmsSeconds = SiteConfig.SMSSENDWAITSECONDS });
            }
            return RenderJsonSuccessResult(false, new { Message = "发送失败：" + smsResult });
        }

        [AllowAnonymous]
        private string GetRegQrCode(Guid guid)
        {
            var sceneId = int.Parse(DateTime.Now.ToString("MMddHHmmss"));
            var senparcWeixinConfig = SenparcDI.GetService<IOptions<SenparcWeixinSetting>>().Value;
            var qrCodeRegCache = SenparcDI.GetService<QrCodeRegCache>();

            while (qrCodeRegCache.MessageCollection.ContainsKey(sceneId.ToString()))
            {
                sceneId++;
            }
            CreateQrCodeResult qrCodeResult = QrCodeApi.Create(senparcWeixinConfig.WeixinAppId, 600, sceneId,
                QrCode_ActionName.QR_SCENE, "reg_code");
            var qrCodeRegData = new QrCodeRegData(sceneId, qrCodeResult.expire_seconds, qrCodeResult.ticket, guid, QrCodeRegDataType.Reg);
            qrCodeRegCache.Insert(qrCodeRegData, qrCodeRegData.Key);
            return qrCodeResult.url;
        }

        public ActionResult Logout()
        {
            _accountService.Logout();
            return Redirect("/");
            //return RedirectToAction("Index", "Home", new { Area = (string)null });
        }
    }
}