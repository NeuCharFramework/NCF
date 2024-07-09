<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />


# NCF - NeuCharFramework
NeuCharFramework(NCF) 是一整套可用于构建基础项目的框架，包含了基础的缓存、数据库、模型、验证及配套管理后台，高度模块化，严格遵循 DDD 设计模式，具有高度的可扩展性。

NCF Web 项目模板：[![Senparc.NCF.Template](https://img.shields.io/nuget/vpre/Senparc.NCF.Template?label=Senparc.NCF.Template)](https://www.nuget.org/packages/Senparc.NCF.Template/)

XNCF 模块模板：[![Senparc.Xncf.XncfBuilder.Template](https://img.shields.io/nuget/vpre/Senparc.Xncf.XncfBuilder.Template?label=Senparc.Xncf.XncfBuilder.Template)](https://www.nuget.org/packages/Senparc.Xncf.XncfBuilder.Template/)


[![Build Status](https://mysenparc.visualstudio.com/NCF%20and%20Senparc.AI%20Cummunity%20Projects/_apis/build/status%2FNeuCharFramework.NCF?branchName=master)](https://mysenparc.visualstudio.com/NCF%20and%20Senparc.AI%20Cummunity%20Projects/_build/latest?definitionId=65&branchName=master)

> **Notice**<br>
> 1. NCF 由盛派（Senparc）团队经过多年优化迭代的自用系统底层框架 SenparcCore 升级而来，经历了 .NET 3.5/4.5 众多系统的实战检验，并最终移植到 .NET Core（同时支持 .NET 6 / .NET 8），目前已在众多系统中稳定运行。感谢大家一直以来的支持！<br>
> 2. 源码中已经附带文档模块，运行 NCF 并安装即可。


<center><img src="https://weixin.senparc.com/images/NCF/login.jpg" /></center>

> 当前快速更新分支：[Developer](https://github.com/NeuCharFramework/NCF/tree/Developer)

> 我们欢迎第三方开源组件提供自己的解决方案，我们将会测试并集成到 NCF 中。


## 在线文档

[https://www.ncf.pub/Docs](https://www.ncf.pub/Docs)

## 当前项目在线 Demo

[https://www.ncf.pub](https://www.ncf.pub)


## QQ 技术交流群

<img src="https://sdk.weixin.senparc.com/images/QQ_Group_Avatar/NCF/QQ-Group.jpg" width="380" />

## 环境要求

- Visual Studio 2019+（建议 2022）或 VS Code 最新版本

- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) 

- 如需查看或修改基础包源代码，请看此项目：https://github.com/NeuCharFramework/NcfPackageSources

> - NCF 默认支持 SQLite、MySQL、SQL Server、PostgreSQL、Oracle、DM（达梦） 等数据库，也可自行实现更多数据库类型。<br>
> - 如使用 SQL Server，最低支持版本为 2012


## 如何安装

### 第一步：打开解决方案

下载或同步本项目代码到本地后，打开 `/src/NCF.sln` 解决方案文件，即可看到 NCF 完整的模板项目：

![run-ncf-01](https://github.com/NeuCharFramework/NCF/assets/2281927/3a150daf-d2a2-438d-a131-6ed98097bc8c)

### 第二步：确认 Senparc.Web 为启动项目

`Senparc.Web` 项目是用于启动 Web 站点的项目，确认已经为启动项目（加粗），如果没有，则点击右键，选择【设为启动项目】。

![run-ncf-02](https://github.com/NeuCharFramework/NCF/assets/2281927/a60ffe04-cdad-470f-96bf-732bb410a2fb)

### 第三步：运行

点击顶部菜单【编译】>【开始执行（不调试）】，或快捷键 <kbd>Ctrl/Command</kbd> + <kbd>F5</kbd>

> 注意：为方便初次运行，默认运行的数据库为 SQLite，如需更换其他数据库，请修改 `Senparc.Web/appsettings.json` 文件中的 `DatabaseType` 值，如 `SqlServer`。

### 完成启动

稍等数秒后，即可完成 NCF Web 项目的启动。

## 首次运行

#### 自动安装

首次运行项目时，系统会对数据库等“基础设施”进行自动安装：

> 注意：默认运行的数据库为 SQL Server，如需更换其他数据库，请查看《使用多数据库》。

<img width="926" alt="install-01" src="https://github.com/NeuCharFramework/NCF/assets/2281927/81e1160c-0171-420c-9457-d5eeffda701d">

点击【立即安装】按钮：
<img width="199" alt="install-02-2" src="https://github.com/NeuCharFramework/NCF/assets/2281927/bb55e18c-76f3-4dc1-ac04-d295960fc541">

#### 高级选项
> 提示：您也可以点击【高级选项】按钮，对数据库、系统名称、默认管理员登录名等进行自定义：<br>
> <img width="827" alt="install-04" src="https://github.com/NeuCharFramework/NCF/assets/2281927/e9763b7c-1a0a-49c7-aa1b-161b3932d15e">


#### 安装成功
阅读提示并点击确认，随后，即可看到安装成功的界面：

![install-03](https://github.com/NeuCharFramework/NCF/assets/2281927/66270938-9248-4808-bcca-d115d387658b)

#### 登录后台

登录后即可进入管理员后台：

![admin-background-01-homepage](https://github.com/NeuCharFramework/NCF/assets/2281927/a72e9e6a-d038-4b8b-bdfd-5c0bb484c49e)


## 待办事项：

- [x] 定制命名空间
- [x] 提供交流社区，包括但不仅限于[问答网站](https://weixin.senparc.com/QA)、[QQ群](#qq-技术交流群)、微信群、直播群。
- [ ] 进一步完善的示例代码和文档。
- [ ] 提供博客和视频教程（也欢迎开发者参与或发起）。
- [ ] 完善 DDD 实践
- [ ] 添加应用商店
- [ ] 提供 CLI 命令行工具
- [ ] 开源 [NeuChar.com](https://www.neuchar.com/) 中的微信功能模块，可使用独立模块集成。

License
--------------
Apache License Version 2.0

```
Copyright 2024 Suzhou Senparc Network Technology Co.,Ltd.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
except in compliance with the License. You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the 
License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
either express or implied. See the License for the specific language governing permissions 
and limitations under the License.
```
Detail: https://github.com/NeuCharFramework/NcfPackageSources/blob/master/LICENSE

100% 开源，支持商用！
