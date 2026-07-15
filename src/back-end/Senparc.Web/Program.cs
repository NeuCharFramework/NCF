/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Program.cs
    文件功能描述：Program 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260705
    修改描述：v0.21.6 重构系统配置初始化与更新流程并统一模型处理

    修改标识：Senparc - 20260705
    修改描述：v0.21.7 重构系统配置初始化与更新流程并统一模型处理
----------------------------------------------------------------*/

//以下数据库模块的命名空间根据需要添加或删除
//using Senparc.Ncf.Database.MySql;         //使用需要引用包： Senparc.Ncf.Database.MySql
//using Senparc.Ncf.Database.Sqlite;        //使用需要引用包： Senparc.Ncf.Database.Sqlite
//using Senparc.Ncf.Database.PostgreSQL;    //使用需要引用包： Senparc.Ncf.Database.PostgreSQL
//using Senparc.Ncf.Database.Oracle;        //使用需要引用包： Senparc.Ncf.Database.Oracle
//using Senparc.Ncf.Database.SqlServer;       //使用需要引用包： Senparc.Ncf.Database.SqlServer

using Senparc.CO2NET;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Database.SqlServer;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Senparc.Web.Controllers;

var builder = WebApplication.CreateBuilder(args);

//添加（注册） NCF 服务（必须）
builder.AddNcf();

//添加 ServiceDefaults
builder.AddServiceDefaults();

#pragma warning disable SYSLIB0014
System.Net.ServicePointManager.ServerCertificateValidationCallback =
    ((sender, certificate, chain, sslPolicyErrors) => true);
#pragma warning restore SYSLIB0014

//添加 Dapr
builder.Services.AddDaprClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Configure request localization (cookie first, then query/header providers)
var supportedCultures = LanguageController.SupportedCultures;
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures)
    .AddInitialRequestCultureProvider(new CookieRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

//Use NCF（必须）
app.UseNcf<BySettingDatabaseConfiguration>();
/*  UseNcf<TDatabaseConfiguration>() 泛型类型说明
 *                
 *                  方法                            |         说明
 * -------------------------------------------------|-------------------------
 *  UseNcf<BySettingDatabaseConfiguration>()        |  由 appsettings.json 决定配置
 *  UseNcf<SqlServerDatabaseConfiguration>()        |  使用 SQLServer 数据库
 *  UseNcf<SqliteMemoryDatabaseConfiguration>()     |  使用 SQLite 数据库
 *  UseNcf<MySqlDatabaseConfiguration>()            |  使用 MySQL 数据库
 *  UseNcf<PostgreSQLDatabaseConfiguration>()       |  使用 PostgreSQL 数据库
 *  UseNcf<OracleDatabaseConfiguration>()           |  使用 Oracle 数据库（V12+）
 *  UseNcf<OracleDatabaseConfigurationForV11>()     |  使用 Oracle 数据库（V11+）
 *  UseNcf<DmDatabaseConfiguration>()               |  使用 DM（达梦）数据库
 *  更多数据库可扩展，依次类推……
 *  
 */

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseFileServer();//非必须

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapDefaultEndpoints();

app.ShowSuccessTip();//显示系统准备成功提示

string GetNcfApiClientPath(string xncfName, string appServiceName, string methodName, string showStaticApiState = null)
{
    var globalName = ApiBindAttribute.GetGlobalName(xncfName, $"{appServiceName}.{methodName}");

    var indexOfApiGroupDot = globalName.IndexOf(".");
    var apiName = globalName.Substring(indexOfApiGroupDot + 1, globalName.Length - indexOfApiGroupDot - 1);
    //var apiBindGlobalName = globalName.Split('.')[0];

    var apiPath = WebApiEngine.GetApiPath(xncfName, appServiceName, apiName, showStaticApiState);
    Console.WriteLine(apiPath);
    return apiPath;
}

/*
Console.WriteLine("============ logMsg =============");
Console.WriteLine("DatabaseName: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseName);
Console.WriteLine("DatabaseType: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseType);
Console.WriteLine("CacheType: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.CacheType);
Console.WriteLine("EnableMultiTenant: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.EnableMultiTenant);
Console.WriteLine("TenantRule: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.TenantRule);
Console.WriteLine("RequestTempLogCacheMinutes: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes);
Console.WriteLine("PasswordSaltToken: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.PasswordSaltToken);
Console.WriteLine("McpAccessToken: " + Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.McpAccessToken);
//output Database connection string
Console.WriteLine("Database connection string: " + string.Join(", ", Senparc.Ncf.Database.Helpers.NcfDatabaseHelper.GetCurrentConnectionInfo().Select(z => $"{z.Key}: {z.Value}")));
Console.WriteLine("Count of Database connection string: " + Senparc.Ncf.Database.Helpers.NcfDatabaseHelper.GetCurrentConnectionInfo().Count());
Console.WriteLine("============ logMsg END =============");
*/

app.MapGet("/test", async httpContext =>
{
    //var senparcWebClient = httpContext.RequestServices.GetService<SenparcWebClient>();
    //var result = await senparcWebClient.GetHtml();
    //await httpContext.Response.WriteAsync(result);

    var apiClientHelper = httpContext.RequestServices.GetService<ApiClientHelper>();
    var apiClient = apiClientHelper.ConnectApiClient("installer");

    var xncfName = "Senparc.Xncf.Installer";//Assembly name / catalog
    var apiBindName = "InstallAppService";
    var methodName = "KeepAlive";
    var apiPath = GetNcfApiClientPath(xncfName, apiBindName, methodName, null);

    //var apiPath = $"/api/{keyName}/{apiBindGroupNamePath}/{apiNamePath}{showStaticApiState}";
    var url = apiPath; //"/api/Senparc.Xncf.Installer/InstallAppService/Xncf.Installer_InstallAppService.KeepAlive";
    var result2 = await RequestUtility.HttpGetAsync(null, url, Encoding.UTF8, apiClient);

    await httpContext.Response.WriteAsync(result2);
});

await app.RunAsync();
