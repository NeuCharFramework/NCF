using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XY.Xncf.SignalR.Hubs;
using XY.Xncf.SignalR.Middlewares;

namespace XY.Xncf.SignalR
{
    public partial class Register : IXncfMiddleware  //需要引入中间件的模块
    {
        #region IXncfMiddleware 接口
        public IApplicationBuilder UseMiddleware(IApplicationBuilder app)
        {
            app.UseSignalRClientMiddleware();
            //app.Use(async (context, next) =>
            //{
            //    //参考如下链接修改返回流
            //    //https://github.com/dotnet/aspnetcore/blob/v3.1.7/src/Middleware/ResponseCompression/src/ResponseCompressionMiddleware.cs

            //    if (context.WebSockets.IsWebSocketRequest)
            //        await next();
            //    else
            //    {
            //        // 保持原来的流
            //        var originalBody = context.Response.Body;

            //        // 用ms替换当前的流
            //        var ms = new MemoryStream();
            //        context.Response.Body = ms;

            //        await next();

            //        // 替换其中的内容
            //        ms.Seek(0, SeekOrigin.Begin);
            //        var reader = new StreamReader(ms);
            //        var str = reader.ReadToEnd();
            //        var appendStr = "";
            //        if (str.Contains("<!DOCTYPE"))
            //        {
            //            appendStr = File.ReadAllText($"{Senparc.CO2NET.Utilities.ServerUtility.AppDomainAppPath}/Shared/SignalR.cshtml");
            //        }
            //        var doubleStr = str + appendStr;
            //        var buffer = Encoding.UTF8.GetBytes(doubleStr);

            //        var ms2 = new MemoryStream();
            //        ms2.Write(buffer, 0, buffer.Length);
            //        ms2.Seek(0, SeekOrigin.Begin);

            //        // 写入到原有的流中
            //        await ms2.CopyToAsync(originalBody);
            //    }
            //});
            //app.Map("/onlineuser", app =>
            //{
            //    app.Run(async context =>
            //    {
            //        var online = File.ReadAllText($"{Senparc.CO2NET.Utilities.ServerUtility.AppDomainAppPath}/Shared/OnlineUser.cshtml");
            //        await context.Response.WriteAsync(online);
            //    });
            //});
            //app.Map("/getonlineuser", app =>
            //{
            //    app.Run(async context =>
            //    {
            //        int pageIndex = 1;
            //        int.TryParse(context.Request.Query["pageIndex"], out pageIndex);
            //        int pageSize = 100;
            //        int.TryParse(context.Request.Query["pageSize"], out pageSize);
            //        string userName = context.Request.Query["userName"];
            //        var allList = GlobleConnectionManage.ClientList
            //           .Where(w => userName == null || userName == "" || w.Value.Any(c => c.UserName != null && c.UserName.Contains(userName)));
            //        var _list = allList
            //                .Skip((pageIndex - 1) * pageSize)
            //                .Take(pageSize)
            //                .ToList();
            //        var userList = new List<KeyValuePair<string, int>>();
            //        foreach (var user in _list)
            //        {
            //            var _userName = string.IsNullOrWhiteSpace(user.Value.First().UserName) ? "匿名" : user.Value.First().UserName;
            //            userList.Add(new KeyValuePair<string, int>(_userName, user.Value.Count));
            //        }
            //        await context.Response.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(new { total = allList.Count(), list = userList }));
            //    });
            //});
            return app;
        }
        #endregion
    }
}
