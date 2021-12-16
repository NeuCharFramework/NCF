using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Database;
//using Senparc.Ncf.Database.MySql;//根据需要添加或删除，使用需要引用包： Senparc.Ncf.Database.MySql
//using Senparc.Ncf.Database.Sqlite;//根据需要添加或删除，使用需要引用包： Senparc.Ncf.Database.Sqlite
//using Senparc.Ncf.Database.PostgreSQL;//根据需要添加或删除，使用需要引用包： Senparc.Ncf.Database.PostgreSQL
using Senparc.Ncf.Database.SqlServer;//根据需要添加或删除，使用需要引用包： Senparc.Ncf.Database.SqlServer
using Senparc.Web;

var builder = WebApplication.CreateBuilder(args);

#region AddDatabase<TDatabaseConfiguration>() 泛型类型说明
/* 
 *                  方法                            |         说明
 * -------------------------------------------------|-------------------------
 *  AddDatabase<SQLServerDatabaseConfiguration>()   |  使用 SQLServer 数据库
 *  AddDatabase<SqliteMemoryDatabaseConfiguration>()|  使用 SQLite 数据库
 *  AddDatabase<MySqlDatabaseConfiguration>()       |  使用 MySQL 数据库
 *  AddDatabase<PostgreSQLDatabaseConfiguration>()  |  使用 PostgreSQL 数据库
 *  更多数据库可扩展，依次类推……
 *  
 */
#endregion


//指定数据库类型（必须）
builder.AddDatabase<SQLServerDatabaseConfiguration>();//默认使用 SQLServer数据库，根据需要改写

//添加（注册） Ncf 服务（必须）
builder.AddNcfServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//Use NCF（必须）
app.UseNcf();

app.UseHttpsRedirection();
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

