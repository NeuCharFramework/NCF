## Migration 文件夹说明

当前文件夹为 AdminSenparcEntities 对象生成的迁移文件，包含系统默认支持的多数据库。


## 手动迁移命令

以 SQLite 为例：

```
dotnet ef migrations add Init -c AdminSenparcEntities_Sqlite -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\Sqlite
```

全部数据库：

```
dotnet ef migrations add Init -c AdminSenparcEntities_Sqlite -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\Sqlite


dotnet ef migrations add Init -c AdminSenparcEntities_MySql -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\MySql


dotnet ef migrations add Init -c AdminSenparcEntities_SqlServer -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\SqlServer


dotnet ef migrations add Init -c AdminSenparcEntities_PostgreSQL -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\PostgreSQL
```