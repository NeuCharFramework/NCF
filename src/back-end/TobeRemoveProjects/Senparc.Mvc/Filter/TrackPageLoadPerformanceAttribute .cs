using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using Senparc.Weixin;

namespace Senparc.Mvc.Filter
{
    /// <summary>
    /// 跟踪页面执行时间
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes",
      Justification = "Unsealed so that subclassed types can set properties in the default constructor or override our behavior.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class TrackPageLoadPerformanceAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 在指定的ActionName下，记录页面时间（*为所有，多个ActionName使用英文,分开）
        /// </summary>
        public string RecordActionName { get; set; }

        private const int WARM_LOAD_SECONDS = 2;//报警时间（秒）

        //创建字典来记录开始时间，key是访问的线程Id.
        private readonly Dictionary<int, DateTime> _start = new Dictionary<int, DateTime>();

        //创建字典来记录当前访问的页面Url.
        private readonly Dictionary<int, string> _url = new Dictionary<int, string>();

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //过滤掉ChildAction, 因为ChildAction实际上不是一个单独的页面

            //COCONET 目前已经没有ChildAction的概念？
            //https://www.davepaquette.com/archive/2016/01/02/goodbye-child-actions-hello-view-components.aspx
            //if (filterContext.IsChildAction)
            //{
            //    return;
            //}


            var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            try
            {
                _start.Add(currentThreadId, DateTime.Now);
                _url.Add(currentThreadId, filterContext.HttpContext.Request == null
                    ? string.Empty
                    : filterContext.HttpContext.Request.AbsoluteUri());
            }
            catch (Exception ex)
            {
                LogUtility.TrackPageLoadPerformance.ErrorFormat(ex.Message, ex);
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (!_start.ContainsKey(currentThreadId)) return;

            try
            {

                //计算出当前页面访问耗时
                var costSeconds = (DateTime.Now - _start[currentThreadId]).TotalSeconds;
                if (costSeconds > 2)//如果耗时超过2秒，就是用log4net打印出，具体是哪个页面访问超过了2秒，具体使用了多长时间。
                {
                    LogUtility.TrackPageLoadPerformance.Warn($"页面执行时间超过{WARM_LOAD_SECONDS}秒。时间：{costSeconds}，地址：{_url[currentThreadId]}");
                }

                if (!RecordActionName.IsNullOrEmpty() &&
                        (RecordActionName == "*" ||
                        RecordActionName.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(z => z.Trim().ToUpper()).Contains(filterContext.RouteData.DataTokens["action"]))
                   )
                {
                    var msg =
                        $"页面【{filterContext.RouteData.Values["Action"]}】执行时间：{costSeconds}秒，地址：{_url[currentThreadId]}";
                    LogUtility.TrackPageLoadPerformance.Debug(msg);
                    WeixinTrace.SendCustomLog("页面性能", msg);
                }
            }
            catch (Exception ex)
            {
                LogUtility.TrackPageLoadPerformance.ErrorFormat(ex.Message, ex);
            }
            finally
            {
                _start.Remove(currentThreadId);
                _url.Remove(currentThreadId);
            }
        }
    }
}
