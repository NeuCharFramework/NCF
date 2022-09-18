using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.Utilities;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Service;
using Senparc.Weixin;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.Helpers;
using Senparc.Weixin.TenPayV3;
using Senparc.Weixin.TenPayV3.Apis;
using Senparc.Weixin.TenPayV3.Apis.BasePay;
using Senparc.Weixin.TenPayV3.Apis.Entities;
using Senparc.Weixin.TenPayV3.Entities;
using Senparc.Weixin.TenPayV3.Helpers;
using Senparc.Xncf.WeixinManagerBase.Domain.Services;
using Senparc.Xncf.WeixinManagerTenPayV3.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManagerTenPayV3.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManagerTenPayV3.OHS.Local.AppService
{
    public class OrderAppService : AppServiceBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ProductService _productService;
        private readonly ServiceBase<Order> _orderService;
        private readonly UserService _userService;
        private readonly BasePayApis _basePayApis;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SenparcHttpClient _httpClient;

        private static TenPayV3Info _tenPayV3Info;
        public static TenPayV3Info TenPayV3Info
        {
            get
            {
                if (_tenPayV3Info == null)
                {
                    var key = TenPayHelper.GetRegisterKey(Senparc.Weixin.Config.SenparcWeixinSetting);

                    _tenPayV3Info =
                        TenPayV3InfoCollection.Data[key];
                }
                return _tenPayV3Info;
            }
        }

        public OrderAppService(IServiceProvider serviceProvider, ProductService productService, ServiceBase<Order> orderService, UserService userService, SenparcHttpClient httpClient, BasePayApis basePayApis, IHttpContextAccessor httpContextAccessor) : base(serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._productService = productService;
            this._orderService = orderService;
            this._userService = userService;
            _httpClient = httpClient;
            _basePayApis = basePayApis;
            this._httpContextAccessor = httpContextAccessor;
        }


        [ApiBind]
        public async Task<AppResponseBase<JsApiUiPackage>> AddAsync(int productId)
        {
            return await this.GetResponseAsync<AppResponseBase<JsApiUiPackage>, JsApiUiPackage>(async (response, logger) =>
            {
                var product = await this._productService.GetObjectAsync(z => z.Id == productId);
                if (product == null)
                {
                    throw new Exception("商品不存在");
                }

                //TODO：检查库存、是否开放等
                var user = _userService.GetObject(z => true);

                //创建订单号（需要检查是否有重复）
                var billNo = string.Format("{0}{1}{2}", TenPayV3Info.MchId/*10位*/, SystemTime.Now.ToString("yyyyMMddHHmmss"),
                      TenPayV3Util.BuildRandomStr(6));

                //调用下单接口下单
                var name = product.Name;
                var price = (int)(product.Price * 100);//单位：分
                var notifyUrl = TenPayV3Info.TenPayV3Notify;

                //请求信息
                TransactionsRequestData jsApiRequestData = new(TenPayV3Info.AppId, TenPayV3Info.MchId, name + " - 微信支付 V3", billNo, new TenpayDateTime(DateTime.Now.AddHours(1), false), null, notifyUrl, null, new() { currency = "CNY", total = price }, new(user.MpOpenId), null, null, null);

                var tenpayV3Setting = Senparc.Weixin.Config.SenparcWeixinSetting.TenpayV3Setting;

                //请求接口
                var basePayApis2 = new Senparc.Weixin.TenPayV3.TenPayHttpClient.BasePayApis2(_httpClient, tenpayV3Setting);
                //var result = await basePayApis2.JsApiAsync(jsApiRequestData);

                //TODO:模拟调用成功
                var result = new JsApiReturnJson()
                {
                    prepay_id = "fakeprepayid",
                    VerifySignSuccess = true
                };

                if (result.VerifySignSuccess != true)
                {
                    throw new WeixinException("获取 prepay_id 结果校验出错！");
                }

                var order = new Order(billNo, productId, user.Id, Domain.Models.OrderState.待支付);

                await _orderService.SaveObjectAsync(order);

                //获取 UI 信息包
                //var jsApiUiPackage = TenPaySignHelper.GetJsApiUiPackage(TenPayV3Info.AppId, result.prepay_id);

                var jsApiUiPackage = new JsApiUiPackage(TenPayV3Info.AppId, "123", "abc", "prepayid=" + result.prepay_id, "signature");

                return jsApiUiPackage;
            });
        }

        [ApiBind]
        [HttpPost]
        public async Task<string> CallbackAsync()
        {
            try
            {
                //获取微信服务器异步发送的支付通知信息
                var resHandler = new TenPayNotifyHandler(_httpContextAccessor.HttpContext);
                var orderReturnJson = await resHandler.AesGcmDecryptGetObjectAsync<OrderReturnJson>();

                //记录日志
                Senparc.Weixin.WeixinTrace.SendCustomLog("PayNotifyUrl 接收到消息", orderReturnJson.ToJson(true));

                //演示记录 transaction_id，实际开发中需要记录到数据库，以便退款和后续跟踪
                var transcationId = orderReturnJson.transaction_id;

                //获取支付状态
                string trade_state = orderReturnJson.trade_state;

                //验证请求是否从微信发过来（安全）
                NotifyReturnData returnData = new();

                //验证可靠的支付状态
                if (orderReturnJson.VerifySignSuccess == true && trade_state == "SUCCESS")
                {
                    returnData.code = "SUCCESS";//正确的订单处理
                    /* 提示：
                        * 1、直到这里，才能认为交易真正成功了，可以进行数据库操作，但是别忘了返回规定格式的消息！
                        * 2、上述判断已经具有比较高的安全性以外，还可以对访问 IP 进行判断进一步加强安全性。
                        * 3、下面演示的是发送支付成功的模板消息提示，非必须。
                        */
                }
                else
                {
                    returnData.code = "FAILD";//错误的订单处理
                    returnData.message = "验证失败";

                    //此处可以给用户发送支付失败提示等
                }

                #region 记录日志（也可以记录到数据库审计日志中）

                var logDir = ServerUtility.ContentRootMapPath(string.Format("~/App_Data/TenPayNotify/{0}", SystemTime.Now.ToString("yyyyMMdd")));
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logPath = Path.Combine(logDir, string.Format("{0}-{1}-{2}.txt", SystemTime.Now.ToString("yyyyMMdd"), SystemTime.Now.ToString("HHmmss"), Guid.NewGuid().ToString("n").Substring(0, 8)));

                using (var fileStream = System.IO.File.OpenWrite(logPath))
                {
                    var notifyJson = orderReturnJson.ToString();
                    await fileStream.WriteAsync(Encoding.Default.GetBytes(notifyJson), 0, Encoding.Default.GetByteCount(notifyJson));
                    fileStream.Close();

                }
                #endregion

                //https://pay.weixin.qq.com/wiki/doc/apiv3/apis/chapter3_1_5.shtml

                //业务逻辑

                var order = await _orderService.GetObjectAsync(z => z.BillNo == orderReturnJson.out_trade_no);

                //判断订单状态，如果不存在或不是待支付状态，则抛出异常
                if (order.OrderState == Domain.Models.OrderState.待支付)
                {
                    order.ChangeOrderState(Domain.Models.OrderState.已支付);
                    await _orderService.SaveObjectAsync(order);
                }
                else
                {
                    throw new WeixinException($"订单状态不正确！billNo：{order.BillNo}，当前状态：{order.OrderState}");
                }

                return returnData.ToJson();
            }
            catch (Exception ex)
            {
                WeixinTrace.WeixinExceptionLog(new WeixinException(ex.Message, ex));
                throw;
            }
        }
    }
}
