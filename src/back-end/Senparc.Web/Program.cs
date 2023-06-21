//以下数据库模块的命名空间根据需要添加或删除
//using Senparc.Ncf.Database.MySql;         //使用需要引用包： Senparc.Ncf.Database.MySql
//using Senparc.Ncf.Database.Sqlite;        //使用需要引用包： Senparc.Ncf.Database.Sqlite
//using Senparc.Ncf.Database.PostgreSQL;    //使用需要引用包： Senparc.Ncf.Database.PostgreSQL
//using Senparc.Ncf.Database.Oracle;          //使用需要引用包： Senparc.Ncf.Database.Oracle
using Senparc.Ncf.Database.SqlServer;       //使用需要引用包： Senparc.Ncf.Database.SqlServer

using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

//添加（注册） Ncf 服务（必须）
builder.AddNcf<SQLServerDatabaseConfiguration>();
/*      AddNcf<TDatabaseConfiguration>() 泛型类型说明
 *                
 *                  方法                            |         说明
 * -------------------------------------------------|-------------------------
 *  AddNcf<SQLServerDatabaseConfiguration>()        |  使用 SQLServer 数据库
 *  AddNcf<SqliteMemoryDatabaseConfiguration>()     |  使用 SQLite 数据库
 *  AddNcf<MySqlDatabaseConfiguration>()            |  使用 MySQL 数据库
 *  AddNcf<PostgreSQLDatabaseConfiguration>()       |  使用 PostgreSQL 数据库
 *  AddNcf<OracleDatabaseConfiguration>()           |  使用 Oracle 数据库（V12+）
 *  AddNcf<OracleDatabaseConfigurationForV11>()     |  使用 Oracle 数据库（V11+）
 *  更多数据库可扩展，依次类推……
 *  
 */

//添加 Dapr
builder.Services.AddDaprClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//Use NCF（必须）
app.UseNcf();

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseFileServer();//为了访问 docs/ 目录，非必须

app.UseCookiePolicy();

app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();
});

app.Run();
