## Senparc.Web.DatabasePlant 项目说明

1、此项目用于使用 `dotnet ef migrations add` 命令时，指定带有特定目标框架的启动项目（如 netcoreapp3.1），这样可以操作任意目标框架（如 netstandard2.1）的项目，而无需进行任何修改；

2、此项目不应该被 Senparc.Web 项目引用，因此不会发布到生产环境，只在开发时使用；

3、必须将需要执行数据库迁移命令的项目引用到此项目中。