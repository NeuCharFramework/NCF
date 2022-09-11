using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AuditLog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public static class IPAddressController
    {
        /// <summary>
        /// 获取客户端Ip
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        [HttpGet]
        //[Anonymous]
        public static string GetClientUserIp(HttpContext httpContext)
        {
            var userHostAddress = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
            //var userHostAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            //最后判断获取是否成功，并检查IP地址的格式
            if (!string.IsNullOrEmpty(userHostAddress) && IsIP(userHostAddress))
            {
                return userHostAddress;
            }
            return string.Empty;
        }

        /// <summary>
        /// 检查IP地址格式
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static bool IsIP(string ip)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
        }
    }

}
