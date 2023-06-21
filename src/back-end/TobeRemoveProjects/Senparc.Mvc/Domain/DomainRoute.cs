//参考文章：http://www.cnblogs.com/wenthink/archive/2013/04/10/MvcDynamicSecondaryDomain.html


namespace Senparc.Mvc
{
    //public class DomainRoute : Route
    //{
    //    private Regex domainRegex;
    //    private Regex pathRegex;

    //    public string Domain { get; set; }
    //    public string AreaName { get; set; }
    //    public string[] Namespaces { get; set; }
    //    public DomainRoute(string domain, string areaName, string url, RouteValueDictionary defaults, string[] namespaces = null)
    //        : base(url, defaults, new MvcRouteHandler())
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public DomainRoute(string domain, string areaName, string url, RouteValueDictionary defaults, IRouteHandler routeHandler, string[] namespaces = null)
    //        : base(url, defaults, routeHandler)
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public DomainRoute(string domain, string areaName, string url, object defaults, string[] namespaces = null)
    //        : base(url, new RouteValueDictionary(defaults), new MvcRouteHandler())
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public DomainRoute(string domain, string areaName, string url, object defaults, IRouteHandler routeHandler, string[] namespaces = null)
    //        : base(url, new RouteValueDictionary(defaults), routeHandler)
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public override RouteData GetRouteData(HttpContextBase httpContext)
    //    {
    //        // 构造regex
    //        domainRegex = CreateRegex(Domain);
    //        pathRegex = CreateRegex(Url);

    //        // 请求信息
    //        string requestDomain = httpContext.Request.Headers["host"];
    //        if (!string.IsNullOrEmpty(requestDomain))
    //        {
    //            if (requestDomain.IndexOf(":") > 0)
    //            {
    //                requestDomain = requestDomain.Substring(0, requestDomain.IndexOf(":"));
    //            }
    //        }
    //        else
    //        {
    //            requestDomain = httpContext.Request.Url.Host;
    //        }
    //        string requestPath = httpContext.Request.AppRelativeCurrentExecutionFilePath.Substring(2) + httpContext.Request.PathInfo;

    //        //匹配域名和路由

    //        Match domainMatch = domainRegex.Match(requestDomain);
    //        Match pathMatch = pathRegex.Match(requestPath);

    //        // Route 数据

    //        RouteData data = null;
    //        if (domainMatch.Success && pathMatch.Success)
    //        {
    //            data = new RouteData(this, RouteHandler);

    //            // 添加默认选项
    //            if (Defaults != null)
    //            {
    //                foreach (KeyValuePair<string, object> item in Defaults)
    //                {
    //                    data.Values[item.Key] = item.Value;
    //                }
    //            }

    //            // 匹配域名路由
    //            for (int i = 1; i < domainMatch.Groups.Count; i++)
    //            {
    //                Group group = domainMatch.Groups[i];
    //                if (group.Success)
    //                {
    //                    string key = domainRegex.GroupNameFromNumber(i);
    //                    if (!string.IsNullOrEmpty(key) && !char.IsNumber(key, 0))
    //                    {
    //                        if (!string.IsNullOrEmpty(group.Value))
    //                        {
    //                            data.Values[key] = group.Value;
    //                        }
    //                    }
    //                }
    //            }

    //            // 匹配域名路径
    //            for (int i = 1; i < pathMatch.Groups.Count; i++)
    //            {
    //                Group group = pathMatch.Groups[i];
    //                if (group.Success)
    //                {
    //                    string key = pathRegex.GroupNameFromNumber(i);
    //                    if (!string.IsNullOrEmpty(key) && !char.IsNumber(key, 0))
    //                    {
    //                        if (!string.IsNullOrEmpty(group.Value))
    //                        {
    //                            data.Values[key] = group.Value;
    //                        }
    //                    }
    //                }
    //            }

    //            //命名空间
    //            if ((Namespaces != null) && (Namespaces.Length > 0))
    //            {
    //                data.DataTokens["Namespaces"] = Namespaces;
    //            }

    //            bool flag = (Namespaces == null) || (Namespaces.Length == 0);
    //            data.DataTokens["UseNamespaceFallback"] = flag;
    //            data.DataTokens["area"] = this.AreaName;

    //            //data.Values["namespaces"] = Namespaces;
    //            //data.Values["namespace"] = Namespaces;
    //        }
    //        return data;
    //    }

    //    public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
    //    {
    //        return base.GetVirtualPath(requestContext, RemoveDomainTokens(values));
    //    }

    //    public DomainData GetDomainData(RequestContext requestContext, RouteValueDictionary values)
    //    {
    //        // 获得主机名
    //        string hostname = Domain;
    //        foreach (KeyValuePair<string, object> pair in values)
    //        {
    //            hostname = hostname.Replace("{" + pair.Key + "}", pair.Value.ToString());
    //        }

    //        // Return 域名数据
    //        return new DomainData
    //        {
    //            Protocol = "http",
    //            HostName = hostname,
    //            Fragment = ""
    //        };
    //    }

    //    private Regex CreateRegex(string source)
    //    {
    //        // 替换
    //        source = source.Replace("/", @"/?");
    //        source = source.Replace(".", @".?");
    //        source = source.Replace("-", @"-?");
    //        source = source.Replace("{", @"(?<");
    //        source = source.Replace("}", @">([a-zA-Z0-9_]*))");

    //        return new Regex("^" + source + "$");
    //    }

    //    private RouteValueDictionary RemoveDomainTokens(RouteValueDictionary values)
    //    {
    //        Regex tokenRegex = new Regex(@"({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?");
    //        Match tokenMatch = tokenRegex.Match(Domain);
    //        for (int i = 0; i < tokenMatch.Groups.Count; i++)
    //        {
    //            Group group = tokenMatch.Groups[i];
    //            if (group.Success)
    //            {
    //                string key = group.Value.Replace("{", "").Replace("}", "");
    //                if (values.ContainsKey(key))
    //                    values.Remove(key);
    //            }
    //        }

    //        return values;
    //    }
    //}
}
