using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Senparc.Areas.Admin.Filter;
using Senparc.Areas.Admin.Models.VD;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using System.Collections.Generic;
using System.Net;

namespace Senparc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SenparcAdminAuthorize()]
    public class BaseAdminController : Controller, IResultFilter
    {
        public BaseAdminController()
        {
        }

        public string UserName
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

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            TempData["Messager"] = TempData["Messager"];

            base.OnActionExecuting(context);
        }


        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (ViewData.Model is IBaseAdminVD)
            {
                IBaseAdminVD vd = ViewData.Model as IBaseAdminVD;
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
                else
                {
                    if (!ModelState.IsValid)
                    {
                        vd.MessagerList.Add(new Messager(MessageType.danger, "提交信息有错误，请检查。"));
                    }
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }



        /// <summary>
        /// 默认转到Edit页面
        /// </summary>
        /// <returns></returns>
        public ActionResult Create()
        {
            return RedirectToAction("Edit");
        }

        #region 成功或错误提示页面

        [NonAction]
        public virtual ActionResult RenderSuccess(string message, string backAction = null, string backController = null, RouteValueDictionary backRouteValues = null)
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
        public virtual ActionResult RenderError(string message)
        {
            //保留原有的controller和action信息
            ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return View("Error", new Error_ExceptionVD
            {
                //HandleErrorInfo = new HandleErrorInfo(new Exception(message), Url.RequestContext.RouteData.GetRequiredString("controller"), Url.RequestContext.RouteData.GetRequiredString("action"))
            });
        }
        [NonAction]
        public ActionResult RenderError404()
        {
            //return RedirectToAction("Error404", "Error", new { aspxerrorpath = Request.Url.PathAndQuery });
            return Error404(Request.PathAndQuery());
        }
        /// <summary>
        /// 显示404页面
        /// </summary>
        /// <param name="aspxerrorpath"></param>
        /// <returns></returns>
        [NonAction]
        public virtual ActionResult Error404(string aspxerrorpath = null)
        {
            if (string.IsNullOrEmpty(aspxerrorpath) && !Request.UrlReferrer().IsNullOrEmpty())
            {
                aspxerrorpath = Request.PathAndQuery();
            }
            Error_Error404VD vd = new Error_Error404VD
            {
                Url = aspxerrorpath
            };
            var result = View("Error404", vd);//HttpNotFound();
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return result;
        }

        #endregion

        /// <summary>
        /// 显示Json结果
        /// </summary>
        /// <param name="success">是否成功</param>
        /// <param name="result">数据，如果失败，可以为字符串的说明</param>
        /// <returns></returns>
        [NonAction]
        public JsonResult RenderJsonSuccessResult(bool success, object result)
        {
            return Json(new
            {
                Success = success,
                Result = result
            });
        }
        public void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }
    }
}
