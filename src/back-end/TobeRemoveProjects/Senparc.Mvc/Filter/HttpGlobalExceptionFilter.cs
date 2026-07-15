using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Models.VD;
using System.Net;

namespace Senparc.Mvc.Filter
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            //context.Result = new ObjectResult(context.Exception);
            //context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            //context.ExceptionHandled = true;
            context.HttpContext.Response.ContentType = "text/html";
            var result = new ViewResult
            {
                ViewName = "Error",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new Error_ExceptionVD()
                    {
                        Message = context.Exception.Message,
                    }
                }
            };
            context.Result = result;
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.ExceptionHandled = true;
        }
    }
}
