using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Senparc.Areas.User.Models.VD;
using Senparc.CO2NET.HttpUtility;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Core.Utility;
using Senparc.File;
using Senparc.Ncf.Log;
using Senparc.Mvc.Filter;
using Senparc.Service;
using Senparc.Ncf.Utility;
using Senparc.Weixin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Senparc.Ncf.Service;
using Senparc.CO2NET.Utilities;

namespace Senparc.Areas.User.Controllers
{
    /// <summary>
    /// 用户管理
    /// </summary>
    /// 
    public class AccountController : BaseUserController
    {
        private readonly AccountService _accountService;
        private readonly AccountPayLogService _accountPayLogService;

        public AccountController(AccountService accountService, AccountPayLogService accountPayLogService)
        {
            _accountService = accountService;
            _accountPayLogService = accountPayLogService;
        }
        [MenuFilter("Account.Account_Index")]
        public IActionResult Index()
        {
            var vd = new Account_IndexVD()
            { };
            return View(vd);
        }

        /// <summary>
        /// 修改基本信息
        /// </summary>
        /// <param name="from"></param>
        /// <param name="file">头像</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Basic(BasicEdit from, IFormFile file)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    SetMessager(MessageType.success, ModelState.FirstErrorMessage());
                    return RedirectToAction("Index");
                }
                string headImg = null;
                if (file != null)
                {
                    headImg = $"/headImgs/headImg_{DateTime.Now.Ticks.ToString()}.{Path.GetExtension(file.FileName)}";
                    await FileExtension.Upload(file, ServerUtility.ContentRootMapPath(headImg));
                }
                _accountService.ChangeBasic(FullAccount.Id, from.RealName, from.Email, headImg);
                SetMessager(MessageType.success, "修改基本信息成功");
            }
            catch (System.Exception ex)
            {
                LogUtility.Account.Error(ex.Message, ex);
                SetMessager(MessageType.success, "修改基本信息失败");
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult PasswordEdit([FromBody] PasswordEdit from)
        {
            if (!ModelState.IsValid)
            {
                return RenderJsonSuccessResult(false, new
                {
                    Message = ModelState.FirstErrorMessage()
                });
            }
            if (!_accountService.CheckPassword(FullAccount.UserName, from.OldPassword))
            {
                return RenderJsonSuccessResult(false, new
                {
                    Message = "旧密码输入错误"
                });
            }
            try
            {
                _accountService.ChangePassword(FullAccount.Id, from.NewPassword);
                return RenderJsonSuccessResult(true, new
                {
                    Message = "修改成功"
                });
            }
            catch (System.Exception ex)
            {
                LogUtility.Account.Error(ex.Message, ex);
                return RenderJsonSuccessResult(false, new
                {
                    Message = "修改密码失败"
                });
            }
        }

        [MenuFilter("Account.Account_Buy")]
        /// <summary>
        /// 购买积分
        /// </summary>
        /// <returns></returns>
        public IActionResult Buy()
        {
            var vd = new Account_BuyVD();
            return View(vd);
        }

        /// <summary>
        /// 获取支付二维码
        /// </summary>
        /// <param name="totalFee">价格(元)</param>
        /// <returns></returns>
        public IActionResult GetPayQr(decimal totalFee)
        {
            try
            {
                //创建订单
                var domain = SiteConfig.IsDebug ? "https://localhost:44306" : "https://weixin.senparc.com";
                var url = $"{domain}/scfPay/getPayQr?totalFee={totalFee}&openId={FullAccount.WeixinOpenId}";
                var orderJson = RequestUtility.HttpPost(url, formData: new Dictionary<string, string>
                {
                    {"totalFee",totalFee.ToString()},
                    {"openId",FullAccount.WeixinOpenId}
                });

                LogUtility.Account.Debug($"OrderJson:{orderJson}");
                var data = JsonConvert.DeserializeObject<OrderJson>(orderJson);
                if (data.return_code != ReturnCode.请求成功)
                {
                    return RenderJsonSuccessResult(false, new { Message = data.return_msg });
                }
                var order = _accountPayLogService.CreateOrder(FullAccount, data.out_trade_no,
                            Request.HttpContext.Connection.RemoteIpAddress.ToString(), totalFee, "积分充值", totalFee,
                            AccountPayLog_PayType.微信支付);
                return RenderJsonSuccessResult(true, new { code_url = data.code_url, orderNumber = order.OrderNumber });
            }
            catch (Exception ex)
            {
                LogUtility.AccountPayLog.Error(ex.Message, ex);
                return RenderJsonSuccessResult(false, new { Message = "获取支付信息失败，请稍后重试" });
            }
        }

        [MenuFilter("Account.Account_PayLog")]
        /// <summary>
        /// 充值记录
        /// </summary>
        /// <returns></returns>
        public IActionResult PayLog(int pageIndex = 1)
        {
            var pageSize = 20;
            var payLogList = _accountPayLogService.GetObjectList(pageIndex, pageSize, z => z.AccountId == FullAccount.Id, z => z.AddTime, OrderingType.Descending);
            var vd = new Account_PayLogVD()
            {
                PayLogList = payLogList
            };

            return View(vd);
        }

        [HttpGet]
        public ActionResult CheckPay(string orderNumber)
        {
            var accountPayLog = _accountPayLogService.GetObject(z => z.OrderNumber == orderNumber);
            return RenderJsonSuccessResult(true, new { status = accountPayLog?.Status == (int)AccountPayLog_Status.已支付 });
        }


        public class OrderJson
        {
            /// <summary>
            /// 状态
            /// </summary>
            public ReturnCode return_code { get; set; }

            public string return_msg { get; set; }
            /// <summary>
            /// 支付二维码内容
            /// </summary>
            public string code_url { get; set; }
            /// <summary>
            /// 商户订单号
            /// </summary>
            public string out_trade_no { get; set; }
        }
    }
}
