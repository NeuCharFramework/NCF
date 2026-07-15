/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Program.cs
    文件功能描述：Program 相关实现
    
    
    创建标识：Senparc - 20241119
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Aspire.Hosting;
using Projects;
using Senparc.Ncf.Core.WebApi;
using System.Text;

var builder = DistributedApplication.CreateBuilder(args);

var installer = builder.AddProject<Senparc_Xncf_Installer>(
        NcfWebApiHelper.GetXncfProjectName<Senparc_Xncf_Installer>(),
        launchProfileName: "https")
    .WithExternalHttpEndpoints();

var accounts = builder.AddProject<Senparc_Xncf_Accounts>(
        NcfWebApiHelper.GetXncfProjectName<Senparc_Xncf_Accounts>(),
        launchProfileName: "Senparc.Xncf.Accounts")
    .WithExternalHttpEndpoints();

var ncfWeb = builder.AddProject<Senparc_Web>(
        NcfWebApiHelper.GetXncfProjectName<Senparc_Web>(),
        launchProfileName: "http")
    .WithReference(installer)
    .WithReference(accounts)
    .WithExternalHttpEndpoints();

// Keep legacy code page support used by existing modules.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Build().Run();
