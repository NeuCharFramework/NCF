using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Mvc.Filter;

namespace Senparc.Mvc
{
    /// <summary>
    /// 强制加HTTPS请求
    /// </summary>
    //[SenparcRequireHttps(Order = -1)]
    public class BaseHttpsRequireController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var httpMethod = httpContext.Request.Method;

            //Log.LogUtility.WebLogger.Error("BaseHttpsRequire url:{0}  Method:{1}, IsIndex:{2},Https:{3}".With(url,
            //    httpMethod, isIndex, isHttps));
            if (httpMethod == "HEAD")
            {
                var isHttps = httpContext.Request.IsHttps;
                if (!isHttps)
                {
                    var url = httpContext.Request.Path; //COCONET 测试此方法
                    var isIndex = url == null || url.ToString() == "/";
                    if (isIndex)
                    {
                        context.Result = Redirect("https://ncf.senparc.com/ ");//根据需要修改
                        //Log.LogUtility.WebLogger.Error("BaseHttpsRequire url:{0}  IP:{1},Method:{2},"
                        //    .With(httpContext.Request.Url, httpContext.Request.UserHostAddress,
                        //        httpContext.Request.HttpMethod));
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
}