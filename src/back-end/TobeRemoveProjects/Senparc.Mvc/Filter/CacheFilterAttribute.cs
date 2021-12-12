namespace Senparc.Mvc
{
    //新的缓存方法参考：https://weblogs.asp.net/imranbaloch/output-caching-in-aspnet5-mvc6

    //public class CacheFilterAttribute : ActionFilterAttribute
    //{
    //    /// <summary>
    //    /// Gets or sets the cache duration in seconds. The default is 10 seconds.
    //    /// </summary>
    //    /// <value>The cache duration in seconds.</value>
    //    public int Duration
    //    {
    //        get;
    //        set;
    //    }

    //    public CacheFilterAttribute()
    //    {
    //        Duration = 10;
    //    }

    //    public override void OnActionExecuted(ActionExecutedContext filterContext)
    //    {

    //        if (Duration <= 0) return;

    //        HttpCachePolicyBase cache = filterContext.HttpContext.Response.Cache;
    //        TimeSpan cacheDuration = TimeSpan.FromSeconds(Duration);

    //        cache.SetCacheability(HttpCacheability.Public);
    //        cache.SetExpires(DateTime.Now.Add(cacheDuration));
    //        cache.SetMaxAge(cacheDuration);
    //        cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
    //    }
    //}

}