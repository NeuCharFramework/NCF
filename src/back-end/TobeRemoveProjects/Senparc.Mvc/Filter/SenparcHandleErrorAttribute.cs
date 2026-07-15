//using Senparc.Ncf.Log;

namespace Senparc.Mvc.Filter
{
    //public class SenparcHandleErrorAttribute : HandleErrorAttribute
    //{
    //    public override void OnException(ExceptionContext filterContext)
    //    {
    //        if (filterContext == null)
    //        {
    //            throw new ArgumentNullException("filterContext");
    //        }

    //        //Record Log
    //        //LogUtility.WebLogger.Error(
    //        //    string.Format("SenparcHandle错误。IP：{0}。页面：{1}",
    //        //        filterContext.HttpContext.Request.UserHostName,
    //        //        filterContext.HttpContext.Request.Url.PathAndQuery),
    //        //    filterContext.Exception);

    //        // If custom errors are disabled, we need to let the normal ASP.NET exception handler
    //        // execute so that the user can see useful debugging information.
    //        if (filterContext.ExceptionHandled || !filterContext.HttpContext.IsCustomErrorEnabled)
    //        {
    //            return;
    //        }

    //        Exception exception = filterContext.Exception;

    //        // If this is not an HTTP 500 (for example, if somebody throws an HTTP 404 from an action method),
    //        // ignore it.
    //        if (new HttpException(null, exception).GetHttpCode() != 500)
    //        {
    //            return;
    //        }

    //        if (!ExceptionType.IsInstanceOfType(exception))
    //        {
    //            return;
    //        }

    //        string controllerName = (string)filterContext.RouteData.Values["controller"];
    //        string actionName = (string)filterContext.RouteData.Values["action"];
    //        //var httpContext = filterContext.HttpContext;
    //        //Log.LogUtility.WebLogger.Error("controller={0} actionName={1} url={2}  IP:{3},Method:{4},"
    //        //    .With(controllerName, actionName, httpContext.Request.Url, httpContext.Request.UserHostAddress, httpContext.Request.HttpMethod));
    //        Error_ExceptionVD vd = new Error_ExceptionVD
    //        {
    //            HandleErrorInfo = new HandleErrorInfo(filterContext.Exception, controllerName, actionName)
    //        };

    //        filterContext.Result = new ViewResult
    //        {
    //            ViewName = View,
    //            MasterName = Master,
    //            ViewData = new ViewDataDictionary<Error_ExceptionVD>(vd),
    //            TempData = filterContext.Controller.TempData,
    //            ViewEngineCollection = new ViewEngineCollection()
    //        };
    //        filterContext.ExceptionHandled = true;
    //        filterContext.HttpContext.Response.Clear();
    //        filterContext.HttpContext.Response.StatusCode = 500;

    //        var url = filterContext.HttpContext.Request.Url.PathAndQuery;

    //        Log.LogUtility.WebLogger.Error("500错误:{0} Url:{1}\r\n{2} IP:{3}".With(exception.Message, url, exception.InnerException != null ? exception.InnerException.StackTrace : null, filterContext.HttpContext.Request.UserHostAddress), exception);

    //        // Certain versions of IIS will sometimes use their own error page when
    //        // they detect a server error. Setting this property indicates that we
    //        // want it to try to render ASP.NET MVC's error page instead.
    //        filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
    //    }
    //}
}
