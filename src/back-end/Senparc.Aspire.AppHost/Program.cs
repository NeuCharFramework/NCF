using System.Text;

var builder = DistributedApplication.CreateBuilder(args);

var ncfWeb = builder.AddProject<Projects.Senparc_Web>("ncf-web", "Senparc.Web");

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddProject("install", "../Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj", opt =>
    {
        opt.LaunchProfileName = "Senparc.Xncf.Installer";
    });

    //builder.AddContainer("my-redis", "redis", "5.0.14");
}

//支持中文字符
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.GetEncoding("GB2312");

builder.Build().Run();
