var builder = DistributedApplication.CreateBuilder(args);

var ncfWeb = builder.AddProject<Projects.Senparc_Web>("ncf-web");

builder.Build().Run();
