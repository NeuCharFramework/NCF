## 说明

此文件夹用于存放 .NET 模板的配置文件

## 资源备忘
项目地址：https://github.com/dotnet/templating

关于排除文件夹的正确做法：https://github.com/dotnet/templating/issues/850#issuecomment-303870563

## 手动生成模板

### 安装模板

> PS E:\ ... \NCF> dotnet new install Senparc.NCF.Template

### 卸载模板

> PS E:\ ... \NCF> dotnet new uninstall Senparc.NCF.Template

### 生成项目

> PS E:\ ...\NCF > dotnet new NCF -n YourProjectName

可选参数：
-n 项目名称
--DB 数据库，配置，格式为：`Database=<Your Database Name>`，如：`Database=MyDb`，默认为 `Database=NCF`
--PasswordSaltToken 密码加密加强选项，此值在首个账号生成后不修改，否则会导致所有密码失效

### 更多使用文档

[从命令行安装（推荐）](https://www.ncf.pub/docs/start/start-develop/get-ncf-template.html#%E4%BB%8E%E5%91%BD%E4%BB%A4%E8%A1%8C%E5%AE%89%E8%A3%85-%E6%8E%A8%E8%8D%90)