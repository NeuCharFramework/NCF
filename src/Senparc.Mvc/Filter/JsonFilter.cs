using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.IO;

namespace Senparc.Mvc.Filter
{
    public class JsonFilter : ActionFilterAttribute
    {
        public string Param { get; set; }
        public Type JsonDataType { get; set; }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.ContentType.Contains("application/json"))
            {
                string inputContent;
                using (var sr = new StreamReader(filterContext.HttpContext.Request.Body))
                {
                    inputContent = sr.ReadToEnd();
                }

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject(inputContent, JsonDataType);
                filterContext.ActionArguments[Param] = result;
            }
        }
    }
}
