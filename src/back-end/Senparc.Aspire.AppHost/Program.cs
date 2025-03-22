using Aspire.Hosting;
using Projects;
using Senparc.Ncf.Core.WebApi;
using System.Text;

var builder = DistributedApplication.CreateBuilder(args);

//if (builder.ExecutionContext.IsRunMode)
//{
var installer = builder.AddProject<Projects.Senparc_Xncf_Installer>(NcfWebApiHelper.GetXncfProjectName<Senparc_Xncf_Installer>())
         .WithExternalHttpEndpoints();

// "../Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj"

//builder.AddContainer("my-redis", "redis", "5.0.14");
//}


var ncfWeb = builder.AddProject<Projects.Senparc_Web>(NcfWebApiHelper.GetXncfProjectName<Senparc_Web>())
           .WithReference(installer)
           .WithExternalHttpEndpoints();


//支持中文字符
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
//Console.InputEncoding = Encoding.UTF8;
//Console.OutputEncoding = Encoding.UTF8;

builder.Build().Run();
