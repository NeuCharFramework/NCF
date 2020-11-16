<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />


# NCF - NeuCharFramework

NeuCharFramework(NCF) 是一整套可用于构建基础项目的框架，包含了基础的缓存、数据库、模型、验证及配套管理后台，模块化，具有高度的可扩展性。

当前版本：`0.2.0-beta6`

> **Notice**<br>
> 1. NCF 由盛派（Senparc）团队经过多年优化迭代的自用系统底层框架 SenparcCore 升级而来，经历了 .NET 3.5/4.5 众多系统的实战检验，并最终移植到 .NET Core（同时支持 .NET 5），目前已在众多系统中稳定运行。感谢大家一直以来的支持！<br>
> 2. 源码中已经附带文档模块，运行 NCF 并安装即可。


<center><img src="https://weixin.senparc.com/images/NCF/login.jpg" /></center>

> 当前快速更新分支：[Developer](https://github.com/NeuCharFramework/NCF/tree/Developer)

> 我们欢迎第三方开源组件提供自己的解决方案，我们将会测试并集成到 NCF 中。


## QQ 技术交流群

<img src="https://sdk.weixin.senparc.com/images/QQ_Group_Avatar/NCF/QQ-Group.jpg" width="380" />

## 环境要求

- Visual Studio 2019+ 或 VS Code 最新版本

> 友情提示：2020年11月发布的 Visual Stuido 2019 v16.8 存在部分bug，暂时不建议升级到此版本。

- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)+ 或 [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0) 

- 如需查看或修改基础包源代码，请看此项目：https://github.com/NeuCharFramework/NcfPackageSources

> - 如使用 EFCore，则需要使用 SQL Server 2012 或以上版本数据库


## 如何安装

### 第一步：打开解决方案

下载或同步本项目代码到本地后，打开 `/src/NCF.sln` 解决方案文件，即可看到 NCF 完整的模板项目：

<img src="https://gitee.com/NeuCharFramework/NcfDocs/raw/master/cn/docs/doc/start/start-develop/images/run-ncf-01.png" />


### 第二步：确认 Senparc.Web 为启动项目

`Senparc.Web` 项目是用于启动 Web 站点的项目，确认已经为启动项目（加粗），如果没有，则点击右键，选择【设为启动项目】。

<img src="https://gitee.com/NeuCharFramework/NcfDocs/raw/master/cn/docs/doc/start/start-develop/images/run-ncf-02.png" />

### 第三步：运行

点击顶部菜单【编译】>【开始执行（不调试）】，或快捷键 <kbd>Ctrl/Command</kbd> + <kbd>F5</kbd>

> 注意：默认运行的数据库为 SQL Server，如需更换其他数据库，请查看《使用多数据库》。

### 完成启动

稍等数秒后，即可完成 NCF Web 项目的启动。

## 首次运行

首次运行项目时，系统会对数据库等“基础设施”进行自动安装：

> 注意：默认运行的数据库为 SQL Server，如需更换其他数据库，请查看《使用多数据库》。

<img src="https://gitee.com/NeuCharFramework/NcfDocs/raw/master/cn/docs/doc/start/start-develop/images/install-01.png" />

点击【立即安装】按钮：
<img src="https://gitee.com/NeuCharFramework/NcfDocs/raw/master/cn/docs/doc/start/start-develop/images/install-02.png" />


阅读提示并点击确认，随后，即可看到安装成功的界面：

<img src="https://gitee.com/NeuCharFramework/NcfDocs/raw/master/cn/docs/doc/start/start-develop/images/install-03.png" />

登录后即可进入管理员后台

<img src="https://gitee.com/NeuCharFramework/NcfDocs/raw/master/cn/docs/doc/start/start-develop/images/admin-background-01-homepage.png" />


## 待办事项：

- [ ] 发布官网及在线 Demo（即将发布）
- [x] 提供完善的项目自动生成服务（参考 [WeChatSampleBuilder](http://sdk.weixin.senparc.com/Home/WeChatSampleBuilder)），为开发者提供项目定制生成服务。
- [x] 提供快捷的模块化开发和安装方法。  
- [ ] 开源 [NeuChar.com](https://www.neuchar.com/) 中的微信功能模块，可使用独立模块集成。
- [ ] 提供完善的示例代码和文档。
- [ ] 提供博客和视频教程（也欢迎开发者参与或发起）。
- [ ] 提供交流社区，包括但不仅限于[问答网站](https://weixin.senparc.com/QA)、[QQ群](#qq-技术交流群)、微信群、直播群。
- [ ] 完善 DDD 实践
- [ ] 添加应用商店
- [x] 定制命名空间
- [x] 发布定制模板生成器（在线）
  - [x] 可定制默认加载模块

