using System;
using DemoAudit.Helpers;
using DemoAudit.Models;
using DemoAudit.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DemoAudit.Filters
{
    public class AuditFilterAttribute : ActionFilterAttribute
    {
        private readonly IAuditRepository _auditRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuditFilterAttribute(IHttpContextAccessor httpContextAccessor, IAuditRepository auditRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _auditRepository = auditRepository;
        }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var objaudit = new AuditModel(); // Getting Action Name 
            var controllerName = ((ControllerBase)filterContext.Controller)
                .ControllerContext.ActionDescriptor.ControllerName;
            var actionName = ((ControllerBase)filterContext.Controller)
                .ControllerContext.ActionDescriptor.ActionName;
            var actionDescriptorRouteValues = ((ControllerBase)filterContext.Controller)
                .ControllerContext.ActionDescriptor.RouteValues;

            if (actionDescriptorRouteValues.ContainsKey("area"))
            {
                var area = actionDescriptorRouteValues["area"];
                if (area != null)
                {
                    objaudit.Area = Convert.ToString(area);
                }
            }

            var request = filterContext.HttpContext.Request;

            if (!string.IsNullOrEmpty(Convert.ToString(filterContext.HttpContext.Session.GetInt32(AllSessionKeys.LangId))))
            {
                objaudit.LangId = Convert.ToString(filterContext.HttpContext.Session.GetInt32(AllSessionKeys.LangId));
            }
            else
            {
                objaudit.LangId = "";
            }

            if (!string.IsNullOrEmpty(Convert.ToString(filterContext.HttpContext.Session.GetInt32(AllSessionKeys.UserId))))
            {
                objaudit.UserId = Convert.ToString(filterContext.HttpContext.Session.GetInt32(AllSessionKeys.UserId));
            }
            else
            {
                objaudit.UserId = "";
            }

            if (!string.IsNullOrEmpty(Convert.ToString(filterContext.HttpContext.Session.GetInt32(AllSessionKeys.RoleId))))
            {
                objaudit.RoleId = Convert.ToString(filterContext.HttpContext.Session.GetInt32(AllSessionKeys.RoleId));
            }
            else
            {
                objaudit.RoleId = "";
            }


            if (!string.IsNullOrEmpty(Convert.ToString(filterContext.HttpContext.Session.GetString(AllSessionKeys.IsFirstLogin))))
            {
                objaudit.IsFirstLogin = Convert.ToString(filterContext.HttpContext.Session.GetString(AllSessionKeys.IsFirstLogin));
            }
            else
            {
                objaudit.IsFirstLogin = "";
            }

            objaudit.SessionId = filterContext.HttpContext.Session.Id; ; // Application SessionID // User IPAddress 
            if (_httpContextAccessor.HttpContext != null)
                objaudit.IpAddress = Convert.ToString(_httpContextAccessor.HttpContext.Connection.RemoteIpAddress);

            objaudit.PageAccessed = Convert.ToString(filterContext.HttpContext.Request.Path); // URL User Requested 

            objaudit.LoginStatus = "A";
            objaudit.ControllerName = controllerName; // ControllerName 
            objaudit.ActionName = actionName;

            RequestHeaders header = request.GetTypedHeaders();
            Uri uriReferer = header.Referer;

            if (uriReferer != null)
            {
                objaudit.UrlReferrer = header.Referer.AbsoluteUri;
            }

            _auditRepository.InsertAuditLogs(objaudit);

        }
    }
}