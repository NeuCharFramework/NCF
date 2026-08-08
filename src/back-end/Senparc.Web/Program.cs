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

    修改标识：Senparc - 20260724
    修改描述：v0.34.0 完善站点本地化及内嵌 WebView 导航兼容性

    修改标识：Senparc - 20260729
    修改描述：v0.34.1 完善站点初始化状态、浏览器导航与多语言提示

    修改标识：Senparc - 20260804
    修改描述：v0.35.0 新增数据库升级维护流程与多平台下载入口

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
using Senparc.Ncf.XncfBase;
using Senparc.Web.Infrastructure.Database;

var databaseUpgradeOptions = DatabaseUpgradeCommandLineOptions.Parse(args);
void ReportDatabaseUpgradeProgress(string message)
{
    // 独立升级命令不会启动 Web 监听，直接输出阶段信息，便于定位连接或模块迁移阻塞。
    if (databaseUpgradeOptions.Enabled)
    {
        Console.WriteLine($"数据库升级：{message}");
    }
}

ReportDatabaseUpgradeProgress("正在创建应用宿主……");
var builder = WebApplication.CreateBuilder(databaseUpgradeOptions.HostArguments);
ReportDatabaseUpgradeProgress("应用构建器创建完成，正在注册 NCF 服务……");

//添加（注册） NCF 服务（必须）
builder.AddNcf();
ReportDatabaseUpgradeProgress("NCF 服务注册完成，正在注册基础服务……");

//添加 ServiceDefaults
builder.AddServiceDefaults();
ReportDatabaseUpgradeProgress("基础服务注册完成，正在注册 Dapr……");

// Keep the platform default TLS certificate validation. A global callback that
// accepts every certificate would make all outbound HTTPS requests vulnerable
// to man-in-the-middle attacks.

//添加 Dapr
builder.Services.AddDaprClient();
ReportDatabaseUpgradeProgress("服务注册完成，正在生成应用宿主……");

var app = builder.Build();
ReportDatabaseUpgradeProgress("应用宿主创建完成，正在注册 XNCF 模块……");

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

// 先完成模块和数据库注册，但延迟启动后台线程，避免旧架构上的后台任务反复失败。
app.UseNcf<BySettingDatabaseConfiguration>(startBackgroundThreads: false);
ReportDatabaseUpgradeProgress("XNCF 模块注册完成，正在检查数据库状态……");
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

var databaseUpgradeCoordinator = app.Services.GetRequiredService<DatabaseUpgradeCoordinator>();
var databaseRuntimeState = await databaseUpgradeCoordinator.InspectAsync();
ReportDatabaseUpgradeProgress($"数据库状态检查完成：{databaseRuntimeState.Status}");
if (databaseUpgradeOptions.Enabled)
{
    ReportDatabaseUpgradeProgress("正在执行待处理的 Migration……");
    var result = await databaseUpgradeCoordinator.UpgradeAsync();
    Console.WriteLine(result.Message);
    foreach (var detail in result.Details)
    {
        Console.WriteLine(detail);
    }

    if (!result.Succeeded)
    {
        Console.Error.WriteLine($"数据库升级失败：{result.State.Message}");
        if (result.State.Exception != null)
        {
            Console.Error.WriteLine(result.State.Exception);
        }
    }

    Environment.ExitCode = result.Succeeded ? 0 : 2;
    await app.DisposeAsync();
    return;
}

if (databaseRuntimeState.Status == DatabaseRuntimeStatus.Ready)
{
    app.StartXncfThreads();
}
else
{
    Console.WriteLine($"数据库状态：{databaseRuntimeState.Status}；{databaseRuntimeState.Message}");
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseFileServer();//非必须

// 架构待升级或数据库不可用时只开放维护页，不让业务请求触发更多数据库异常。
app.UseDatabaseMaintenanceMode();

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapDefaultEndpoints();

if (databaseRuntimeState.Status == DatabaseRuntimeStatus.Ready)
{
    app.ShowSuccessTip();//显示系统准备成功提示
}

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

// This route is a local development smoke test and must not be published by
// the deployed site. It calls an internal installer API and exposes its result.
if (app.Environment.IsDevelopment())
{
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
}

await app.RunAsync();
