using Aspire.Hosting;
using Projects;
using Senparc.Ncf.Core.WebApi;
using System.Text;

var builder = DistributedApplication.CreateBuilder(args);


var accounts = builder.AddProject<Projects.Senparc_Xncf_Accounts>(NcfWebApiHelper.GetXncfProjectName<Senparc_Xncf_Accounts>())
        .WithExternalHttpEndpoints();

var installer = builder.AddProject<Projects.Senparc_Xncf_Installer>(NcfWebApiHelper.GetXncfProjectName<Senparc_Xncf_Installer>())
        .WithReference(accounts)
        .WithExternalHttpEndpoints();

installer.WithReference(installer);

var ncfWeb = builder.AddProject<Projects.Senparc_Web>(NcfWebApiHelper.GetXncfProjectName<Senparc_Web>())
        .WithReference(accounts)
        .WithReference(installer)
        .WithExternalHttpEndpoints();


//支持中文字符
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.GetEncoding("GB2312");

builder.Build().Run();
