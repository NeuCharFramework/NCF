## Migration 文件夹说明

当前文件夹为 SystemEntities 对象生成的迁移文件，包含系统默认支持的多数据库。


## 手动迁移命令

以 SQLite 为例：

```
dotnet ef migrations add ReBuildDDD -c BasePoolEntities_Sqlite -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Service\Domain\Migrations\Sqlite
```

全部数据库：

```
dotnet ef migrations add Init -c BasePoolEntities_Sqlite -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Service\Domain\Migrations\Sqlite


dotnet ef migrations add Init -c BasePoolEntities_MySql -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Service\Domain\Migrations\MySql


dotnet ef migrations add Init -c BasePoolEntities_SqlServer -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Service\Domain\Migrations\SqlServer


dotnet ef migrations add Init -c BasePoolEntities_PostgreSQL -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Service\Domain\Migrations\PostgreSQL
```