using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.HttpUtility;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.WebApi;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.SqlServer;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.AreasBase;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddControllers().AddDapr();

//激活 Xncf 扩展引擎（必须）
var logMsg = builder.StartWebEngine(new[] { "Senparc.Areas.Admin" });
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
app.UseXncfModules(registerService, senparcCoreSetting.Value)
   .UseNcfDatabase<SqlServerDatabaseConfiguration>();
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

app.MapRazorPages();
app.MapControllers();

app.MapDefaultEndpoints();

app.MapGet("/aspire-test1", async httpContext =>
{
    var xncfName = "Senparc.Xncf.Accounts";//Assembly name / catalog

    var apiClientHelper = httpContext.RequestServices.GetService<ApiClientHelper>();
    var apiClient = apiClientHelper.ConnectApiClient(NcfWebApiHelper.GetXncfProjectName(xncfName));

    var apiBindName = "AccountAppService";
    var methodName = "KeepAlive";
    var apiPath = NcfWebApiHelper.GetNcfApiClientPath(xncfName, apiBindName, methodName, null);
    Console.WriteLine("/test url: " + apiPath);
    //var apiPath = $"/api/{keyName}/{apiBindGroupNamePath}/{apiNamePath}{showStaticApiState}";
    var url = apiPath;
    var result2 = await RequestUtility.HttpGetAsync(null, url, Encoding.UTF8, apiClient);

    await httpContext.Response.WriteAsync(result2);
});

app.MapGet("/aspire-test2", async httpContext =>
{
    var xncfName = "Senparc.Xncf.Installer";//Assembly name / catalog
    var apiClientHelper = httpContext.RequestServices.GetService<ApiClientHelper>();
    var apiClient = apiClientHelper.ConnectApiClient(NcfWebApiHelper.GetXncfProjectName(xncfName));
    var url = "/TestInstall";
    var result2 = await RequestUtility.HttpGetAsync(null, url, Encoding.UTF8, apiClient);

    await httpContext.Response.WriteAsync(result2);
});

app.Run();
