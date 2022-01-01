using log4net.Config;

using log4net;
using Microsoft.AspNetCore.Builder;

using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.SqlServer;

using System.IO;

using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET;

var builder = WebApplication.CreateBuilder(args);
//指定数据库（必须）
builder.Services.AddDatabase<SQLServerDatabaseConfiguration>();
builder.Services.AddControllers().AddDapr();

//激活 Xncf 扩展引擎（必须）
var logMsg = builder.Services.StartWebEngine(builder.Configuration, builder.Environment);
//如果不需要启用 Areas，可以只使用 services.StartEngine() 方法

Console.WriteLine("============ logMsg =============");
Console.WriteLine(logMsg);
Console.WriteLine("============ logMsg END =============");

builder.Services.AddSwaggerGen();


//读取Log配置文件
var repository = LogManager.CreateRepository("NETCoreRepository");
XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
//Use NCF（必须）
IOptions<SenparcCoreSetting> senparcCoreSetting = app.Services.GetService<IOptions<SenparcCoreSetting>>();
IOptions<SenparcSetting> senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();

// 启动 CO2NET 全局注册，必须！
// 关于 UseSenparcGlobal() 的更多用法见 CO2NET Demo：https://github.com/Senparc/Senparc.CO2NET/blob/master/Sample/Senparc.CO2NET.Sample.netcore3/Startup.cs
var registerService = app
    //全局注册
    .UseSenparcGlobal(app.Environment, senparcSetting.Value, globalRegister => { });


//XncfModules（必须）
Senparc.Ncf.XncfBase.Register.UseXncfModules(app, registerService, senparcCoreSetting.Value);
//using (var scope = app.Services.CreateScope())
//{

//    foreach (var register in Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList)
//    {
//        await register.InstallOrUpdateAsync(scope.ServiceProvider, Senparc.Ncf.Core.Enums.InstallOrUpdate.Install);
//    }
//}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();
});

app.Run();
