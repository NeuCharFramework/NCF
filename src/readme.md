## 文件夹说明

文件夹名                    |       说明
---------------------------|---------------------
[back-end](./back-end)     | 后端源码文件夹
[front-end](./front-end)   | 前端源码文件夹
NCF.Template.csproj        | 模板项目文件，此文件用于项目维护团队生成 NCF 项目模板，开发过程请忽略此文件


## 分支说明

当前最新代码请见分支：[Developer](https://github.com/NeuCharFramework/NCF/tree/Developer)。

## 远程安装模板

```bash
X:\NCF\src\> dotnet new install Senparc.NCF.Template
```

## 本地安装模板

1. 使用 Release 编译当前项目

2. 使用命令行进入 BuildOutPut 文件夹，运行（以当前版本为 v0.12.0 为例）：

```bash
X:\NCF\src\BuildOutPut> dotnet new install .\Senparc.NCF.Template.0.12.0.nupkg
```