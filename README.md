<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />


# NCF - NeuCharFramework

NeuCharFramework(NCF) 是一整套可用于构建基础项目的框架，包含了基础的缓存、数据库、模型、验证及配套管理后台，模块化，具有高度的可扩展性。

当前版本：`0.2.0-beta6`

> 说明：NCF 由盛派（Senparc）团队经过多年优化迭代的自用系统底层框架 SenparcCore 整理而来，经历了 .NET 3.5/4.5 众多系统的实战检验，并最终移植到 .NET Core，目前已在多个 .NET Core 系统中稳定运行，在将其转型为开源项目的过程中，需要进行一系列的重构、注释完善和兼容性升级，目前尚处于雏形阶段，希望大家多提意见，我们会争取在最短的时间内优化并发布第一个试用版（Preview1）。感谢大家一直以来的支持！<br>
> <br>
> Preview1 版本中，我们将提供更加完善的模块化架构和辅助工具，当前源码已经可用于学习和测试使用。


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


## 待办事项：


- [ ] 发布官网及在线 Demo（即将发布）
- [x] 提供完善的项目自动生成服务（参考 [WeChatSampleBuilder](http://sdk.weixin.senparc.com/Home/WeChatSampleBuilder)），为开发者提供项目定制生成服务。
- [x] 提供快捷的模块化开发和安装方法。  
- [ ] 开源 [NeuChar.com](https://www.neuchar.com/) 中的微信功能模块，可使用独立模块集成。
- [ ]提供完善的示例代码和文档。
- [ ]提供博客和视频教程（也欢迎开发者参与或发起）。
- [ ]提供交流社区，包括但不仅限于[问答网站](https://weixin.senparc.com/QA)、[QQ群](#qq-技术交流群)、微信群、直播群。
- [ ] 完善 DDD 实践
- [ ] 添加应用商店
- [x] 定制命名空间
- [ ] 发布定制模板生成器（在线）
  - [x] 可定制默认加载模块

