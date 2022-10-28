using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.XncfBase;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddDapr();

//激活 Xncf 扩展引擎（必须）
var logMsg = builder.StartWebEngine<SQLServerDatabaseConfiguration>();
//如果不需要启用 Areas，可以只使用 services.StartEngine() 方法

Console.WriteLine("============ logMsg =============");
Console.WriteLine(logMsg);
Console.WriteLine("============ logMsg END =============");

builder.Services.AddSwaggerGen();

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
app.UseXncfModules(registerService, senparcCoreSetting.Value);
//using (var scope = app.Services.CreateScope())
//{

//    foreach (var register in Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList)
//    {
//        await register.InstallOrUpdateAsync(scope.ServiceProvider, Senparc.Ncf.Core.Enums.InstallOrUpdate.Install);
//    }
//}

app.Map("/TestInstall", () => ("this is a text from Dapr Container." + SystemTime.Now));

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
