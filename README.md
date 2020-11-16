<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />


# NCF - NeuCharFramework

NeuCharFramework(NCF) 是一整套可用于构建基础项目的框架，包含了基础的缓存、数据库、模型、验证及配套管理后台，模块化，具有高度的可扩展性。

当前版本：`0.1.0-beta4`

> 说明：NCF 由盛派（Senparc）团队经过多年优化迭代的自用系统底层框架 SenparcCore 整理而来，经历了 .NET 3.5/4.5 众多系统的实战检验，并最终移植到 .NET Core，目前已在多个 .NET Core 系统中稳定运行，在将其转型为开源项目的过程中，需要进行一系列的重构、注释完善和兼容性升级，目前尚处于雏形阶段，希望大家多提意见，我们会争取在最短的时间内优化并发布第一个试用版（Preview1）。感谢大家一直以来的支持！<br>
> <br>
> Preview1 版本中，我们将提供更加完善的模块化架构和辅助工具，当前源码已经可用于学习和测试使用。


<center><img src="https://weixin.senparc.com/images/NCF/login.jpg" /></center>

> 当前快速更新分支：[Developer](https://github.com/NeuCharFramework/NCF/tree/Developer)

> 我们欢迎第三方开源组件提供自己的解决方案，我们将会测试并集成到 NCF 中。



NCF 除了会为大家提供完善的框架代码，我们还在着手：


1. [x] 提供完善的项目自动生成服务（参考 [WeChatSampleBuilder](http://sdk.weixin.senparc.com/Home/WeChatSampleBuilder)），为开发者提供项目定制生成服务。

1. [x] 提供快捷的模块化开发和安装方法。  

1. [ ] 开源 [NeuChar.com](https://www.neuchar.com/) 中的微信功能模块，可使用独立模块集成。

1. [ ]提供完善的示例代码和文档。

1. [ ]提供博客和视频教程（也欢迎开发者参与或发起）。

1. [ ]提供交流社区，包括但不仅限于[问答网站](https://weixin.senparc.com/QA)、[QQ群](#qq-技术交流群)、微信群、直播群。

## QQ 技术交流群

<img src="https://sdk.weixin.senparc.com/images/QQ_Group_Avatar/NCF/QQ-Group.jpg" width="380" />

## 环境要求

- Visual Studio 2019+ 或 VS Code 最新版本

- .NET Core 3.1+ ，SDK下载地址：https://dotnet.microsoft.com/download/dotnet-core/3.1

- 如需查看或修改基础包源代码，请看此项目：https://github.com/NeuCharFramework/NcfPackageSources

> - 如使用 EFCore，则需要使用 SQL Server 2012 或以上版本数据库

## 如何安装

安装过程可以选择极客型命令行安装方式，或全自动一键完成。

### 第一步：准备数据库
确保已经安装 SQL Server 2012 及以上版本，系统登录用户具有数据库创建权限（可以不需要使用sa等账号登录），如果必须要使用账号登录，[请看这里](https://github.com/NeuCharFramework/NCF/wiki/%E5%A6%82%E4%BD%95%E4%BF%AE%E6%94%B9%E9%BB%98%E8%AE%A4%E6%95%B0%E6%8D%AE%E5%BA%93%E8%BF%9E%E6%8E%A5%E5%AD%97%E7%AC%A6%E4%B8%B2%EF%BC%9F)

### 第二步：下载源码
1. 同步源代码到本地后，使用 Visual Studio 打开 `/src/NCF.sln`

### 第三步：自动安装

 1. 将 `Senparc.Web` 项目设为启动项目，并运行，地址如：https://localhost:44311/


 未初始化之前，访问首页会自动显示安装界面：
 
<img src="https://weixin.senparc.com/images/NCF/Installs/03.1.png" />

点击【立即安装】按钮：

<img src="https://weixin.senparc.com/images/NCF/Installs/03.2.png" />

> 如果您未手动执行【第二步】，系统将为您在后台自动执行相关步骤。

随后，系统将自动完成初始化安装，此时页面会提示默认管理员账号和密码，请记住这两个参数，以后将无法明文找回！

<img src="https://weixin.senparc.com/images/NCF/Installs/03.3.png" />


 4. 点击【点击这里登录】，使用已经保存好的管理员账号及密码进行登录：


<img src="https://weixin.senparc.com/images/NCF/Installs/04.png" />


5. 完成登录

<img src="https://weixin.senparc.com/images/NCF/Installs/05.png" />




## 模块化开发

NCF 支持“即插即用”的模块化开发模式，您可以面向模块（或领域）进行开发，然后使用 NCF 对模块进行一键集成。

NCF 的扩展模块代号为 `XNCF`。

### 如何加载模块？

#### 第一步：加载 XNCF 扩展模块 dll

找到 XNCF 包并加载 dll 或 从 nuget 安装到 NCF 项目，例如我们官方的一个示例（ChangeNamespace），用于定制修改整个项目的命名空间：https://www.nuget.org/packages/Senparc.Xncf.ChangeNamespace

<img src="https://weixin.senparc.com/images/NCF/XncfModules/01.png" />

> 说明：`Senparc.Xncf.ChangeNamespace` 模块在 NCF 源码中已默认装载，无需从 nuget 下载。

#### 第二步：进入管理员后台的 【扩展模块】 > 【模块管理】

此时可已经可以看到系统自动扫描到了新增模块：

<img src="https://weixin.senparc.com/images/NCF/XncfModules/02.png" />


#### 第三步：点击所需安装模块的【安装按钮】，即可轻松完成模块安装

点击【安装】按钮后，自动进入到模块详情页面，大功告成！

<img src="https://weixin.senparc.com/images/NCF/XncfModules/03.png" />

> 模块默认晚装完毕后，会进入“新增待审核”的状态，点击进行开启即可。

### 如何使用已安装的模块？

在上一步模块详情页中，列出了当前模块支持的所有功能的列表：

<img src="https://weixin.senparc.com/images/NCF/XncfModules/04.png" />

> 为了让每个模块更加沟通安全，每一个功能都可以预先看到所有参数的透视说明

选择需要执行的方法，点击左侧【执行】按钮。例如点击“修改命名空间”功能的执行【执行】按钮：

<img src="https://weixin.senparc.com/images/NCF/XncfModules/05.png" />

按照提示输入对应的参数，点击【运行】按钮，即可执行此模块的此项功能。

执行完成后可以看到提示信息：

<img src="https://weixin.senparc.com/images/NCF/XncfModules/06.png" />

程序中所有命名空间已被批量修改：

<img src="https://weixin.senparc.com/images/NCF/XncfModules/07.png" />


<strong>此过程中无需编写一行代码！</strong>


### 如何更新模块？

您只需要引用最新的 dll（或安装 nuget 包），系统后台将自动扫描到版本更新：

`【模块管理】页面`

<img src="https://weixin.senparc.com/images/NCF/XncfModules/08.png" />

或 `模块详情页面`

<img src="https://weixin.senparc.com/images/NCF/XncfModules/09.png" />

点击【立即安装】即可完成新版本的升级！

### 如何自己开发模块？

NCF 的扩展模块代号为 `XNCF`，您可以在 [NcfPackageSources](https://github.com/NeuCharFramework/NcfPackageSources) 项目中找到我们的模块 [示例项目](https://github.com/NeuCharFramework/NcfPackageSources/tree/master/src/Extensions/Senparc.Xncf.ChangeNamespace)，只需要“依葫芦画瓢”即可。后续稳定版发布之后将有完整的配套文档。


### 是否可以不使用模块化开发？

> Q：我的系统比较简单，也不需要考虑移植、弹性或扩展，是否可以直接在 NCF 上进行开发？

> A：当然可以。加载模块只是为了方便重构和系统解耦使用而已，NCF 适用于大多数开发场景，包括单体应用、分布式、容器、微信等移动端、跨平台 Hybrid 应用，等等。


## 待办事项：
- [ ] 完善 DDD 实践
- [ ] 添加应用商店
- [x] 定制命名空间
- [ ] 发布官网及在线 Demo
- [ ] 发布定制模板生成器（在线）
  - [x] 可定制默认加载模块

