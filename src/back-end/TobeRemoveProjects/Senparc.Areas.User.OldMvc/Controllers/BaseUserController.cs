using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Senparc.Areas.User.Models.VD;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Mvc.Controllers;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.User.Controllers
{
    [Area("User")]
    [UserAuthorize("UserOnly")]
    public class BaseUserController : BaseController, IResultFilter
    {
        // public List<Neural> CurrentNeurals { get; set; }
        public new string UserName
        {
            get
            {
                if (User.Identity.IsAuthenticated)
                {
                    return User.Identity.Name;
                }
                return null;
            }
        }

        public bool IsLogined
        {
            get
            {
                //是否已经登录
                var authenticate = HttpContext.AuthenticateAsync(UserAuthorizeAttribute.AuthenticationScheme)
                    .GetAwaiter().GetResult();

                return authenticate.Succeeded;
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            TempData["Messager"] = TempData["Messager"];
            //var userService = SenparcDI.GetService<AccountService>();

            base.OnActionExecuting(context);
        }

        public new void OnResultExecuting(ResultExecutingContext context)
        {
            if (ViewData.Model is IBaseUserVD vd)
            {
                vd.UserName = UserName;
                vd.RouteData = RouteData;
                vd.MessagerList = vd.MessagerList ?? new List<Messager>();
                var tmpStr = (string)TempData["Messager"];
                if (!string.IsNullOrWhiteSpace(tmpStr))
                {
                    var obj = JsonConvert.DeserializeObject<Messager>(tmpStr);
                    if (obj != null)
                    {
                        vd.MessagerList.Add(obj);
                    }
                }
                //else
                //{
                //    if (!ModelState.IsValid)
                //    {
                //        vd.MessagerList.Add(new Messager(MessageType.danger, "提交信息有错误，请检查。"));
                //    }
                //}
            }
            base.OnResultExecuting(context);
        }

        public new void OnResultExecuted(ResultExecutedContext context)
        {
            base.OnResultExecuted(context);
        }

        [NonAction]
        public override ActionResult RenderSuccess(string message, string backAction = null, string backController = null, RouteValueDictionary backRouteValues = null)
        {
            backAction = backAction ?? RouteData.Values["controller"] as string;
            backController = backController ?? RouteData.Values["controller"] as string;
            backRouteValues = backRouteValues ?? new RouteValueDictionary();
            SuccessVD vd = new SuccessVD()
            {
                Message = message,
                BackAction = backAction,
                BackController = backController,
                BackRouteValues = backRouteValues
            };
            return View("Success", vd);
        }

        [NonAction]
        public override ActionResult RenderError(string message, Exception ex = null)
        {
            //保留原有的controller和action信息
            ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return View("Error", new Error_ExceptionVD() { Message = message, Exception = ex });
        }

        /// <summary>
        /// 显示Json结果
        /// </summary>
        /// <param name="success">是否成功</param>
        /// <param name="result">数据，如果失败，可以为字符串的说明</param>
        /// <param name="jsonSerializerSettings"></param>
        /// <param name="jsonpCallbackFunction"></param>
        /// <returns></returns>
        [NonAction]
        public new ActionResult RenderJsonSuccessResult(bool success, object result,
            Newtonsoft.Json.JsonSerializerSettings jsonSerializerSettings = null,
            string jsonpCallbackFunction = null)
        {
            var json = new
            {
                Success = success,
                Result = result
            };
            if (!jsonpCallbackFunction.IsNullOrEmpty())
            {
                return Content($"{jsonpCallbackFunction}({json.ToJson()})");
            }
            //return Json(json, jsonRequestBehavior);
            //https://stackoverflow.com/questions/4155014/json-asp-net-mvc-maxjsonlength-exception
            //解决JSON JavaScriptSerializer 进行序列化或反序列化时出错。字符串的长度超过了为 maxJsonLength 属性设置的值

            jsonSerializerSettings = jsonSerializerSettings ?? new Newtonsoft.Json.JsonSerializerSettings();
            return new JsonResult(json);
        }

        public new void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }
    }
}
