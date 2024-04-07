// See https://aka.ms/new-console-template for more information
using System.Text.RegularExpressions;

Console.WriteLine("即将开始 v0.8.0 NCF 模板升级，请将 NCF 模板项目升级到 v0.8.0 版本后，此程序将自动修改文件请先做好备份！点击任意键继续。");

Console.ReadKey();

string srcDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../src/");
ProcessDirectory(srcDirectory);

Console.WriteLine("升级完成，点击任意键退出。");

Console.ReadKey();
static void ProcessDirectory(string targetDirectory)
{
    // 处理当前目录下的文件
    string[] fileEntries = Directory.GetFiles(targetDirectory);
    foreach (string fileName in fileEntries)
    {
        ProcessFile(fileName);
    }

    // 递归处理子目录
    string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
    foreach (string subdirectory in subdirectoryEntries)
    {
        ProcessDirectory(subdirectory);
    }
}

static void ProcessFile(string path)
{
    Console.WriteLine($"Processing {path}...");
    string content = File.ReadAllText(path);
    bool modified = false;

    // 模式1：处理XXSenparcEntities_<Database>.cs文件
    if (Regex.IsMatch(Path.GetFileName(path), @"^\w+SenparcEntities_\w+\.cs$"))
    {
        content = content.Replace("using Microsoft.AspNetCore.Builder;", "using Microsoft.Extensions.DependencyInjection;");
        content = Regex.Replace(content, @"protected override Action<IServiceCollection> ServicesAction => services =>", "protected override Action<IApplicationBuilder> AppAction => app =>");
        modified = true;
    }

    // 模式2：全局文件替换
    if (content.Contains("services.AddDatabase("))
    {
        content = content.Replace("services.AddDatabase(", "app.UseNcfDatabase(");
        modified = true;
    }

    // 模式3：SQLServer配置名称更改
    if (content.Contains("SQLServerDatabaseConfiguration"))
    {
        content = content.Replace("SQLServerDatabaseConfiguration", "SqlServerDatabaseConfiguration");
        modified = true;
    }

    // 模式4：启动引擎配置更新
    if (content.Contains("builder.StartWebEngine<TDatabaseConfiguration>();"))
    {
        content = content.Replace("builder.StartWebEngine<TDatabaseConfiguration>();", "builder.StartWebEngine();");
        content = content.Replace("app.UseNcf();", "app.UseNcf<TDatabaseConfiguration>();");
        modified = true;
    }

    // 如果文件被修改，则保存更改
    if (modified)
    {
        File.WriteAllText(path, content);
        Console.WriteLine($"{path} has been modified.");
    }
}