using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.XncfBase;
using XY.Xncf.SignalR.Hubs;

namespace XY.Xncf.SignalR
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => "XY.Xncf.SignalR";

        public override string Uid => "E83B5062-025E-4F6B-A247-62B779E0D4C9";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1.0";//必须填写版本号

        public override string MenuName => "SignalR终端用户";

        public override string Icon => "fa fa-joomla";

        public override string Description => "基于SignalR打通服务端与客户端，可基于此基础之上根据业务扩展。";

        public override IList<Type> Functions => new Type[] { };
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration)
        {
            #region 内置的 JWT 身份验证
            //services.AddAuthentication(options =>
            //{
            //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //})
            //.AddJwtBearer(options =>
            //{
            //    options.Authority = ""/* TODO: Insert Authority URL here */;
            //    options.Events = new JwtBearerEvents
            //    {
            //        OnMessageReceived = context =>
            //        {
            //            var accessToken = context.Request.Query["access_token"];

            //            var path = context.HttpContext.Request.Path;
            //            if (!string.IsNullOrEmpty(accessToken) &&
            //                (path.StartsWithSegments("/loginHub")))
            //            {
            //                context.Token = accessToken;
            //            }
            //            context.HttpContext.Items["clientId"] = context.Token;
            //            return Task.CompletedTask;
            //        }
            //    };
            //});
            #endregion

            services.AddSignalR();

            return base.AddXncfModule(services, configuration);
        }
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseSignalR(route =>
            {
                route.MapHub<LoginHub>("/loginHub");
            });
            app.Use(async (context, next) =>
            {
                //参考如下链接修改返回流
                //https://github.com/dotnet/aspnetcore/blob/v3.1.7/src/Middleware/ResponseCompression/src/ResponseCompressionMiddleware.cs

                if (context.WebSockets.IsWebSocketRequest)
                    await next();
                else
                {
                    // 保持原来的流
                    var originalBody = context.Response.Body;

                    // 用ms替换当前的流
                    var ms = new MemoryStream();
                    context.Response.Body = ms;

                    await next();

                    // 替换其中的内容
                    ms.Seek(0, SeekOrigin.Begin);
                    var reader = new StreamReader(ms);
                    var str = reader.ReadToEnd();
                    var appendStr = "";
                    if (str.Contains("<!DOCTYPE"))
                    {
                        appendStr = File.ReadAllText($"{Senparc.CO2NET.Utilities.ServerUtility.AppDomainAppPath}/Shared/SignalR.cshtml");
                    }
                    var doubleStr = str + appendStr;
                    var buffer = Encoding.UTF8.GetBytes(doubleStr);

                    var ms2 = new MemoryStream();
                    ms2.Write(buffer, 0, buffer.Length);
                    ms2.Seek(0, SeekOrigin.Begin);

                    // 写入到原有的流中
                    await ms2.CopyToAsync(originalBody);
                }
            });
            app.Map("/onlineuser", app =>
            {
                app.Run(async context =>
                {
                    var online = File.ReadAllText($"{Senparc.CO2NET.Utilities.ServerUtility.AppDomainAppPath}/Shared/OnlineUser.cshtml");
                    await context.Response.WriteAsync(online);
                });
            });
            app.Map("/getonlineuser", app =>
            {
                app.Run(async context =>
                {
                    int pageIndex = 1;
                    int.TryParse(context.Request.Query["pageIndex"], out pageIndex);
                    int pageSize = 100;
                    int.TryParse(context.Request.Query["pageSize"], out pageSize);
                    string userName = context.Request.Query["userName"];
                    var allList = GlobleConnectionManage.ClientList
                       .Where(w => userName == null || userName == "" || (w.Value != null && w.Value.Any(c => c.UserName != null && c.UserName.Contains(userName))));
                    var _list = allList
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();
                    var userList = new List<KeyValuePair<string, int>>();
                    foreach (var user in _list)
                    {
                        var _userName = string.IsNullOrWhiteSpace(user.Value.First().UserName) ? "匿名" : user.Value.First().UserName;
                        userList.Add(new KeyValuePair<string, int>(_userName, user.Value.Count));
                    }
                    await context.Response.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(new { total = allList.Count(), list = userList }));
                });
            });
            return base.UseXncfModule(app, registerService);
        }
    }
}
