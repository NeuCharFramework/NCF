using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MySql;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Xncf.Accounts.Models;
using System;
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDaprClient();
builder.Services.AddControllers().AddDapr();

//指定数据库（必须）
builder.Services.AddDatabase<SQLServerDatabaseConfiguration>();
//激活 Xncf 扩展引擎（必须）
var logMsg = builder.Services.StartWebEngine(builder.Configuration, builder.Environment);
//如果不需要启用 Areas，可以只使用 services.StartEngine() 方法

Console.WriteLine("============ logMsg =============");
Console.WriteLine(logMsg);
Console.WriteLine("============ logMsg END =============");


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
//Use NCF（必须）
IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();

//启动 CO2NET 全局注册，必须！
//关于 UseSenparcGlobal() 的更多用法见 CO2NET Demo：https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
var registerService = app.UseSenparcGlobal(app.Environment);

//XncfModules（必须）
Senparc.Ncf.XncfBase.Register.UseXncfModules(app, registerService, senparcCoreSetting.Value);

using (var scope = app.Services.CreateScope())
{
    foreach (var register in Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList)
    {
        await register.InstallOrUpdateAsync(scope.ServiceProvider, Senparc.Ncf.Core.Enums.InstallOrUpdate.Install);
    }
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();

app.Map("/Test2", _app =>
{
    _app.Use(async (HttpContext context, RequestDelegate next) =>
    {
        var sr = new StreamWriter(context.Response.Body);
        await sr.WriteLineAsync("OK");
        await sr.FlushAsync();
    });
});

app.Map("/TestDB", _app =>
{
    _app.Use(async (HttpContext context, RequestDelegate next) =>
    {
        using (var scope = context.RequestServices.CreateScope())
        {
            var accountCount = 0;
            try
            {
                var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(typeof(Senparc.Xncf.Accounts.Register));
                var ctx = scope.ServiceProvider.GetService(dbContextType) as AccountSenparcEntities;
                accountCount = ctx.Set<Account>().Count();
                Console.WriteLine("accountCount:" + accountCount);
            }
            catch (global::System.Exception ex)
            {
                global::System.Console.WriteLine(ex.ToString());
            }
            finally
            {
                var sr = new StreamWriter(context.Response.Body);
                await sr.WriteLineAsync("accountCount:" + accountCount);
                await sr.FlushAsync();
            }
        }
    });
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();
});


app.Run();
