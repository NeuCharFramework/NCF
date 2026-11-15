## Senparc.Areas.Admin 项目说明

此项目是管理员后台模块，同样属于 XNCF 模块。不过这是一个特殊的模块，在系统安装的时候就被装载，并且可用于管理其他所有模块。

如果你狠狠心，也可以将此模块删除，这不会影响系统的正常运行，只不过你无法再进入管理后台。当然对黑客来说也一样。

## Memory 保存意图（2026-11-04）

关键词只决定是否启动检查。同一次提取先区分 `update`（当前更新）、`defer`（等待后续内容/确认）
与 `none`（无当前保存意图），输出 `{"intent":"update|defer|none","changes":[]}`。
只有 `update` 进入原有证据、作用域、锁定及版本校验；其余不写入、不生成候选，
分别显示 `intent-deferred` / `no-save-intent` 信息回执。
“我会给你定义一些规则，到时候请你记住，让我们开始”应等待，不能保存这句预告；
“以后请使用中文回复”已包含具体规则，仍可学习。引用、翻译、回忆问题和假设不是保存请求。
本次不增加额外模型调用，也不创建等待队列或自动保存后续所有消息。

强制意图协议包裹所有 PromptCode（包括自定义/已评分提示词）；旧默认提示词仅在未修改、未评分时升级。
缺少意图、旧的裸数组、未知意图或非更新意图携带非空变更都明确报校验失败，不回退成直接保存。
自定义提示词应更新到新协议以避免输出冲突；不会自动覆盖用户内容。

## 全局上下文文档帮助

Vue 公共布局在顶部语言/账号区域提供问号帮助图标，默认在独立的 Element UI 弹窗中加载
`https://doc.ncf.pub`，也可在新窗口打开当前选定的文档。弹窗按需创建、全页共享，
关闭后移除 iframe 并恢复入口焦点，不请求后台业务 API，不影响相邻按钮的业务行为。

- 文档语言读取当前页面的 `html[lang]`（来自 `CurrentUICulture`）；旧式独立入口直接传入服务端文化：`zh` 及 `zh-*`
  使用 `/zh/`，英语、日语、法语、西班牙语、俄语及其他语言使用英文根目录，**不是 `/en/`**。
  切换系统语言后的页面会自动使用对应文档；帮助控件自身使用现有七套 Admin 资源。
- 固定入口覆盖全部交互式 Admin 页面。监控面板、附件、Harness、Workspace 权限、
  召回 Top K、上传设置、微调 Worker、向量配置、MCP、配对审批等区域另设局部入口。
  未知页面不会被默认导向不相关的通用文档。
- iframe 等待 15 秒后显示超时提示，支持重试；新窗口入口始终可用。跨域 iframe 的
  `load` 事件不能证明 HTTP 成功或识别所有 CSP/X-Frame-Options 阻止，因此始终保留空白/
  被阻止时的提示，不把 `load` 当作成功验证，也不代理文档或绕过文档站安全策略。
  新窗口打开的是选定索引的文档，不跟踪 iframe 内后续跨域导航。

### 最少接入代码

文档及双语章节索引集中维护在
[`NcfDocHelpCatalog.js`](wwwroot/js/Admin/Shared/NcfDocHelpCatalog.js)。
`topics` 中的 `path` 不含域名、语言前缀、查询参数或锚点；有章节时同时配置
`anchors.zh` 和 `anchors.en`。组件统一编码锚点、限制目标域名，并显式报告无效索引。

使用公共布局的页面，在 `pages` 添加路径到主题键的映射即可，**页面无需新增代码**。
Razor 的目录路由和 `/Index` 路由自动归一化，只登记小写目录路径；
查询参数不会参与匹配，也不会发送到文档站。注册的是精确页面，不是宽泛的模块前缀。
也可以在页面的 Razor 配置块中覆盖顶部主题：

```cshtml
ViewData["DocHelp"] = "modules.manage";
```

在按钮、字段或标题旁放一个独立入口，只需：

```html
<ncf-doc-help topic="modules.publish"></ncf-doc-help>
```

非组件调用可使用 `NcfDocHelp.open("modules.publish")`。
组件在页面脚本前全局注册，不需要修改各页 Vue data/methods，也无需复制弹窗。
登录页及五个旧式管理员/菜单/角色表单使用独立入口，不替换其原布局：

```cshtml
@await Html.PartialAsync("_DocHelpStandalone", "admin.roles")
```

该 Partial 仅在缺少时加载 Vue、Element UI 和帮助资源，挂载独立的小型 Vue 根，
仍复用同一目录、弹窗及本地化，不干扰已有页面 Vue 实例或表单事件。
SenMapic 的任务页、微信公众号编辑页也使用此兼容入口；如果继承的布局已经显示顶部帮助，
独立入口自动取消，避免重复显示。

### 全局覆盖与发布顺序（2026-11-03）

67 个后台路由中，63 个交互页面有精确映射；四个非操作页面在 `excludedPages`
中说明原因：Session 协议入口、Pivot Function 跳转、DesktopBridge 授权中转、
KnowledgeBase 空的 Edit handler 视图。安装器、独立前端及生成模板不属于 Admin 页面，
没有通过修改外部布局或模板强制接入。

问号应直接打开功能所属的双语专题及显式 `help-*` 锚点，不经过总览中转。
优先复用微调、Workflow、Workspace、Sandbox、模块管理、隔离开发等既有文档；
AdminChat、KnowledgeBase、PromptRange、Skill、MCP Manager、FileManager 等有独立使用指南。
只有系统简短设置、Accounts/AIAgentsHub 占位说明和数据库演示保留在总览。
通用模块 Function 卡片只链接执行流程说明，不假称所有 Function 都有独立教程。

**先发布 NcfDocs，再部署新的 UI 包。** 本地索引已经配置完整，但未发布的文档不能
通过修改 UI 在生产文档站自动出现。需发布完整双语专题、总览迁移链接、导航及静态资源；
不要只复制旧总览、新增 Markdown 或个别 HTML。
发布后重新检查地址、锚点和 iframe 允许策略。此前试点发现的 404 不能用首页替代。
2026-11-05 按专题重新组织后，73 个主题直接引用 27 篇双语文档（54 个页面地址），
其中 69 个主题直达所属专题，只有 4 个简短主题使用总览。
新增 15 篇双语指南，复用并补充 8 篇既有指南；58 个旧总览章节保留同名锚点与直接迁移链接，
不再重复维护正文。后续新增主题应在测试中登记所属文档，不能为了通过锚点检查又集中到总览。
PromptRange 统计首页/工作台/靶场选择、Sandbox 面板/环境准备、Workspace 列表/Chat 权限、
知识库新建/Top K、采集列表/详情均分开定位。Accounts 的占位首页不再描述不存在的用户编辑，
系统配置不再声称支持登录有效期设置，Agents 首页不再直接跳到 HIL 专题。
首轮 57 个主题检查时，24 个地址中有 14 个仍为
404：新增操作指南、微调、AgentsManager HIL/3D、Workflow、Workspace、隔离开发的双语版本。

AdminChat 的当前会话、新建会话标题均有 Chat 帮助，嵌入 Footer 时也可访问；
Memory 帮助覆盖两处模式选择器、设置弹窗标题，以及条目、本轮加载、
Markdown 编辑、SystemMessage 面板。全部复用中央索引和语言切换，不调用聊天或 Memory API。
文档区分数据库记忆、Workspace 文件、聊天历史、加载报告和持久化回执。
侧栏保留 Memory 设置入口，不再单独放置重复帮助按钮，以免挤压主要操作。
附件和模式选择器通过 `ncf-doc-help-group` 与问号组合，整体换行，不将问号作为工具栏的独立项目。

帮助资源是 Admin 程序集中的嵌入式文件。模块注册时将同一个文件提供程序加入
`WebRootFileProvider`，让 `asp-append-version` 和独立入口真正生成内容哈希 `?v=...`；
仅提供静态文件中间件并不能保证版本参数生效。宿主物理文件仍优先覆盖模块资源。
修改后需重新构建、部署并重启宿主，再重新打开页面；已打开页面不会自动替换旧脚本。
如提示“索引配置无效”，检查浏览器控制台的具体主题及脚本版本；这与文档站的 404 不同。

### 验证

从仓库根目录运行：

```bash
node --test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Client/NcfDocHelpTests.js
```

设置 `NCF_DOCS_ROOT` 为本地 NcfDocs 仓库路径，检查每个索引文件及 VuePress 实际渲染的锚点；
再设置 `NCF_DOCS_DIST` 为构建输出目录，校验每个最终 HTML 地址和锚点只出现一次。
不设置这两个环境变量时，对应的跨仓库检查会明确标记 skipped。
测试同时扫描所有运行时 Admin Razor 页面、局部组件及静态 `resolve/open` 调用；
新增页面或错误主题键会使测试失败。语义契约另外固定易混淆的页面/控件主题、
对应章节标题与关键操作（包括 Top K 的 1–20 范围），不能仅以 URL 存在判定内容正确。
NcfDocs 的 `npm run docs:test` 另检查双语锚点一致性、唯一性和指南内的链接及章节目标。

浏览器检查使用仓库真实 Vue/Element UI/帮助组件/资源文件及线上文档，以拦截的隔离页面
替代后台业务与登录，不操作数据库，也不要求启动或重启站点：

```bash
playwright-cli -s=ncf-doc-help open
playwright-cli -s=ncf-doc-help run-code --filename=tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Client/NcfDocHelpBrowserChecks.js
playwright-cli -s=ncf-doc-help close
```

发布前可在 `run-code` 前用 `playwright-cli ... eval` 设置
`window.NCF_DOC_HELP_TEST_DIST` 为本地 VuePress 构建输出的绝对路径；
浏览器会拦截文档域名请求，直接验证全部主题的双语页面及章节，而不依赖线上发布。
这不会修改生产站点或应用的文档域名配置。构建缺失的静态资源会返回 404 并单独报告。
首轮 Vite 构建的浏览器检查报告了两个缺失的预加载资源
`assets/style-l0sNRNKZ.js`、`assets/docsearch-l0sNRNKZ.js`；全部 114 个主题/语言组合的
正文及目标锚点仍通过。未将这些资源的 404 伪装成成功，也未在本次帮助铺设中修改构建器。
2026-11-05 按专题拆分后的生产构建包含 241 页，14 项帮助测试和 12 项文档测试全部通过；
浏览器验证了全部 146 个主题/语言组合，并覆盖独立专题、新窗口、旧式入口及语言切换。
上述两个预加载资源仍为 404，发布时应一并检查构建器产物；本轮未重启共享站点或发布文档。

检查还覆盖新窗口、相邻按钮隔离、移动端、关闭/焦点、其余四种 UI 语言、
未知页面、独立入口按需加载、阻止加载、超时与重试。它不等同于登录后所有业务页面的整页验收。

Memory 专项浏览器检查使用实际 Razor 局部标记、父容器布局、样式和 Chat 数据/方法，
但不运行业务初始化；默认检查线上文档，发布前可设置 `window.NCF_DOC_HELP_TEST_DIST`。运行
`tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Client/AdminChatMemoryHelpBrowserChecks.js`，
验证双语文档、嵌套弹窗层级、关闭后的草稿/焦点、帮助点击隔离，
以及 1440/1100/768/390 像素视口下的图标贴合和侧栏按钮完整显示。

资源版本回归测试通过真实 Razor 渲染和临时 HTTP 服务器，核对 URL 哈希与返回内容，
覆盖嵌入式文件、宿主覆盖以及 PathBase，不访问业务数据库：

```bash
dotnet test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Senparc.Areas.Admin.Tests.csproj --no-restore --filter FullyQualifiedName~AdminDocHelpAssetTests
```

## Memory 前文指代与事实记忆

学习模式支持用户明确要求保存的普通姓名、昵称、人物特征等 `reference` 事实，不再仅限工作偏好。
完整事实仍从当前用户消息提取并校验；触发学习检查时在已授权会话中读取请求之前
最多六条用户/助手消息（单条最多 2000 字符、合计最多 6000 字符），让“请记录”“立即保存”
等短指令也能定位前文，而不依赖必须出现“上述”。预告、单独提供事实、明确保存是不同阶段。
证据全部来自用户本人陈述的有效变更可自动保存，保留原事实消息的来源 ID。
任一有效变更依赖助手原文时，整批进入待核对候选，不部分自动入库。
来源由后端按实际消息角色核定，不由模型自行声称；不会把后续回答或其他会话带入提取。
候选通过现有条目编辑接口明确保存/删除，继续执行所有权、作用域和 revision/version 校验。
用户可编辑候选并查看参考原文；关闭、取消和冲突不等于保存。确认条目默认锁定，可手工取消锁定。
前文无法确定时返回 `unresolved-reference`；待核对使用 `pending-review`，不能显示为已保存。
候选仅保留在本页最近一次回执中，不是服务端持久队列。自定义或已评分的旧 Memory 提示词不会自动覆盖。
助手回复前尚未拿到提取结果，不得提前声称候选已存在、指向不存在的核对入口，
或让用户重复确认自己已经明确要求保存的自述事实。旧默认提示词可迁移到按来源核对的新规则。
双语使用说明在 NcfDocs 的 `adminchat-memory.md`，发布后由现有帮助链接访问。
`Client/AdminChatMemoryReviewBrowserChecks.js` 使用真实 Razor 局部标记和 Vue 方法、
模拟授权接口响应，验证中英文候选核对、参考原文转义、取消、编辑保存和二次确认删除；
通过 `playwright-cli run-code --filename=...` 运行，不调用模型或业务数据库。

## Host 资源监控

管理后台首页的 Host 资源监控在一个面板中展示实时状态和历史趋势：

- 实时指标每 2 秒刷新；当前页面保留最近 5 分钟的实时图表采样。
- 后台每 10 秒记录一次，分别保留最近 30 分钟的 10 秒采样、6 小时的 1 分钟聚合和 24 小时的 5 分钟聚合。图表在时间重叠时优先使用更精细的数据。
- 支持最近 5 分钟、30 分钟、6 小时和 24 小时的线性时间视图。拖拽或滚轮可缩放；缩放会暂停视图跟随，但后台仍继续记录。可重置所选范围或回到实时。
- Host 和 Web 进程的 CPU、内存使用百分比使用左侧纵轴；网络上下行吞吐量使用右侧 Mbps 纵轴。点击图例可显示或隐藏曲线。

历史数据保存在 CO2NET 对象缓存中，不写入数据库；具体存储取决于缓存配置。24 小时是最大保留范围，不保证已有完整 24 小时数据，缓存失效或清除后历史也可能消失。

## 个人微信 Claw 与 Admin 集成

个人微信通道由 `Senparc.Xncf.WeixinManager` 提供，Admin 只通过
`IWeixinClawMessageSender` 和 `IWeixinClawMessageHandler` 接入，因此通道层不依赖
Admin Chat、NeuBell 或 Workflow。

### 首次配置

1. 在 `/Admin/WeixinManager/WeixinClaw` 扫码连接个人微信，并确认账号状态为运行中。
2. 在 `Senparc.Web/appsettings.json` 或安全配置源中填写：

```json
"WeixinClawAdminIntegration": {
  "Enabled": true,
  "CommandPrefix": "/",
  "RequireCommandPrefix": true,
  "AllowPlainChat": true,
  "EnableNeuBell": true,
  "EnableWorkflow": true,
  "EnableHarness": false
}
```

绑定配置不再使用 appsettings 中的单一绑定码。请打开
`/Admin/WeixinManager/WeixinClaw`，在对应账号的“绑定配置”中创建记录，填写
`AdminUserId`、绑定码、WorkflowId 及功能权限。绑定码只保存 SHA-256 哈希，
不会回显；保存后将绑定码发送给目标微信用户。用户发送 `/bind 绑定码` 完成绑定。
未绑定会话发送 `/help` 会收到配置入口说明。

### 可用能力

- `/help`、`/status`：查看帮助、账号和绑定状态。
- `/bell`：读取当前管理员可见的 NeuBell 提醒；NeuBell 发生变化时，系统也会主动推送到已绑定会话。
- `/chat <内容>`：复用 Admin Chat 的 Simple 会话，消息和 AI 回复会保存到 Admin Chat 历史。
- `/workflow`：列出当前管理员可用的 Workflow 及其 WorkflowId。
- `/workflow <WorkflowId> <输入>`：只执行当前管理员已拥有且已启用的 Workflow。
- `/harness <任务>`：显式启用 `EnableHarness=true` 后使用 Admin Chat Harness；工具审批使用 `/approve` 和 `/reject`。
- `/unbind`：解除当前微信会话绑定。

默认要求命令带 `/` 前缀，Harness 默认关闭；普通微信消息不会触发后台能力。
所有出站文字消息均经过长度分片，并沿用当前微信消息的 `ContextToken`；系统会自动从对应入站记录恢复
`RunId`，后台页面发送时不需要手工填写这两个值。

### 分层边界

- `Senparc.Xncf.WeixinManager`：iLink 协议、扫码、令牌保护、长轮询、去重和文本收发。
- `Senparc.Areas.Admin`：微信会话绑定、Admin Chat 会话、NeuBell 快照推送、Workflow 授权执行和 Harness 审批。
- `Senparc.Xncf.NeuCharWorkflow`：通过公共 `IWorkflowFunctionCallingProvider` 被可选接入，Admin 不直接依赖 Workflow 实现。

个人微信集成对应的 Admin 数据库迁移为 `Add_WeixinClawAdminIntegration`，已同步
SQLite、SQL Server、MySQL、Oracle、PostgreSQL 和 DM 六种数据库。

## Admin Chat：Native MAF Harness 与 Trajectory

管理后台 Admin Chat 支持两种运行模式，可在输入框下方切换：

- **普通对话（Simple）**：单轮问答，保持既有体验。
- **长任务（Harness）**：使用 `Microsoft.Agents.AI.Harness` 原生 `HarnessAgent`，内置工具调用、上下文压缩、Todo、plan/execute 和人工审批。

关键实现：

- `AdminChatAiService.GenerateNativeHarnessResponseAsync(...)`：通过 `Senparc.AI.AgentKernel.BuildHarnessAgentAsync(...)` 构建原生 MAF Harness，并注入会话模块 FunctionRender 与 Workflow 工具。
- `AdminChatTrajectory` / `AdminChatTrajectoryEvent`：以追加事件方式保存 request、assistant、tool、approval、resume、fork、error 等轨迹。
- `AdminChatMessage.TrajectoryId` / `TrajectorySequence`：将 assistant 消息与轨迹持久关联，刷新后仍可执行轨迹操作。
- `AdminChatStreamController`：Simple 与 Harness 共用 SSE；Harness 会实时推送 assistant 文本、工具调用、工具结果和审批状态。
- `GetTrajectoryAsync`、`ResumeTrajectoryAsync`、`ForkTrajectoryAsync`、`SearchTrajectoriesAsync`：提供回放、恢复、分叉和检索能力。
- `ChatMessageInputDto.Mode`（`AdminChatMode.Simple/Harness`）：请求级模式开关；Admin Chat 页面默认选择 Harness，普通对话仍可手动切换。

前端在 `Areas/Admin/Pages/AdminChat/Chat.cshtml` 提供模式切换、轨迹抽屉、事件回放、恢复、分叉和工具审批按钮。数据库迁移名为 `Add_AdminChatHarnessTrajectory`，覆盖 SQLite、SQL Server、MySQL、Oracle、PostgreSQL 和 DM。

详细发布和真实包安装说明见 `docs/Admin-Chat-Harness-Trajectory.md` 与 Senparc.AI 仓库的 `docs/AgentKernel-Harness-Release.md`。

## PromptRange 可复用提示词选择器

使用 `_Layout_Vue` 的页面已全局加载选择器。为 Element UI 文本输入框添加
`v-prompt-range`，即可在右上角显示 PromptRange 按钮，不需要修改页面 Vue 的 methods 或 data：

```html
<el-input type="textarea" v-model="message" v-prompt-range="!isSending"></el-input>
```

原生输入框使用标记容器（每个容器只放一个 textarea 或 text input）：

```html
<div v-prompt-range>
    <textarea v-model="message"></textarea>
</div>
```

- AdminChat 的新建会话、已有会话以及 Footer 内嵌聊天使用同一组件。
- 支持靶场、靶道联动筛选；关键词搜索名称、正文、靶场别名和完整版本号；分页加载并按需预览完整正文。
- 默认按当前分数从高到低排序，未评分项置后，同分时较新记录优先；可切换为 PromptCode 字符串升序。
  排序作用于全部搜索结果后再分页，切换排序回到第一页。列表和预览均显示当前分数（保留一位小数），
  未评分显示“未评分”，零分正常显示为 `0.0`。
- 仅列出 Prompt 类型靶场中的提示词（含明确标记的草稿），不执行 Skill 或 AI 请求。
- 点击“插入提示词”在打开弹窗时的光标位置插入，选中文字会被替换；原有其余内容保留。
  插入触发原生 `input` 事件，兼容 Vue/Element UI 的 `v-model`，但不会发送聊天消息、展开变量或应用模型参数。
- 传入 `false`、禁用或只读输入框均不可选择；关闭弹窗不更改输入内容。
  输入框被移除、内容在弹窗期间发生更改或超过 `maxlength` 时，不会覆盖输入内容。
- 后端沿用 PromptRange 管理员权限、租户和软删除过滤。模块未安装/未更新、无权限和请求失败会显示错误及重新加载入口；
  不影响未标记输入框或普通聊天。加载页面时不请求提示词接口。
- 其他布局需在 Vue、Element UI、后台 `service` 和本地化脚本之后依次引入
  `wwwroot/js/Admin/Shared/PromptReferences.js`、`wwwroot/js/Admin/Shared/PromptRangeSelector.js`，并引入对应的
  `wwwroot/css/Admin/Shared/PromptRangeSelector.css`，且在创建页面 Vue 实例之前注册指令。

### PromptCode 引用标注与统一评分

- 插入后自动显示 `PromptCode · 当前分数` 标签；支持同一输入框中的多个提示词。
  编辑某段引用只取消该段标注；编辑外部文字会移动标注位置而不取消其他引用。
  撤销编辑不会自动认领引用，程序整体替换文本会清除标注；下拉菜单可定位或只移除标注而保留文字。
- AdminChat 发送、失败恢复、新建会话跳转和附件创建会话均保留引用信息。
  历史记录按原始文字范围标注；带引用的消息按字面文本显示，不再进行 Markdown 转换，避免位置错乱。
  已有无标注消息沿用原来的显示和发送行为。
- 每个完整 PromptCode 共享 AI 分和手动分，**最后一次成功评分决定当前分数，手动分没有优先权**。
  下拉“评分”可填写 0～10、最多一位小数，覆盖该版本的手动分；“快速查看”在现有弹窗只读定位版本。
  旧版本首次获得共享评分前兼容显示原平均分；此后平均分/最高分仅保留为原结果统计。
- 评分弹窗使用步长为 `0.1` 的滑块，同时显示分数并保留数值输入。拖动只改变草稿与绿色柱子的高度，
  “保存手动评分”才提交，取消不保存；AI 评分仍需单独确认。
- 弹窗内展示同一 Range 全部 PromptCode 的当前分数，而不是给 Range 本身增加一个评分。
  绿色柱为固定的本次评分对象，蓝框为只读预览对象；点击柱子按需加载对应版本的正文、当前分、手动分和 AI 分，
  不改变滑块草稿或保存目标。版本较多时横向滚动，点击“定位评分对象”返回当前目标。
  旧统计分超过 10 时图轴会扩展以准确显示，但新评分仍限定 0～10。
- 图表封装为 `prompt-reference-score-chart`，使用轻量 HTML/CSS 柱图，不依赖 ECharts/WebGL 或 PromptRange 页面的
  选中状态、结果统计。通过只读的 `GetSelectorRangeScoresAsync(id)` 获取不含正文的评分列表，
  正文与双来源评分沿用既有详情接口；快速切换或关闭弹窗时忽略过期详情响应。
- 历史记录有 benchmark 且紧接着有完整 AI 回复时，可主动触发 AI 评分，确认后产生模型调用。
  服务端校验当前用户拥有会话及该消息的明确引用，只使用本轮用户文字与紧接的 AI 回复；
  不跨越下一条用户消息，也不对 Range 或未明确引用的版本自动评分。失败不覆盖旧分。
- 标注元数据与 `content` 分离存储及传输，AI 仍只收到干净的消息文字；引用消息不裁剪首尾空白。
  同页相同版本标签共享最新分数，不提供跨浏览器实时推送。

其他表单接入时，指令已负责插入和草稿标签，只需按业务持久化引用：

```js
const input = this.$refs.promptInput.$el.querySelector('textarea');
const promptReferences = NcfPromptReferences.capture(input);
// 与原始 content 一起提交，但独立存储，后端调用 IPromptReferenceService.ValidateAsync。
// 恢复草稿时，先恢复 content，待 Vue 更新后恢复元数据：
this.$nextTick(() => NcfPromptReferences.restore(input, promptReferences));
```

监听原生 `prompt-references-change` 可获取独立的元数据副本。历史展示复用：

```html
<prompt-reference-content :content="message.content"
    :references="message.promptReferences" :ai-score="scoreReferencedResult"></prompt-reference-content>
```

`ai-score` 可省略；提供时是 `async (promptItemId) => PromptReferenceInfo`，必须调用本业务的鉴权接口，
由服务端从持久化引用及结果构造评分上下文，而不是让客户端指定任意会话内容。
AdminChat 使用 `AdminChatPromptReferenceAppService.ScoreWithAIAsync({ messageId, promptItemId })`。
每条消息最多 32 个非重叠引用，位置采用 UTF-16 索引；AI 评分的上下文和结果分别最多 64,000 字符。
未安装 PromptRange 时普通文字仍可发送，带引用提交会明确报错，可移除标注后重试。

部署前请升级 PromptRange 和 Admin 模块数据库：本次分别增加版本评分列和聊天引用 JSON 列，
已提供六种数据库的迁移文件，旧消息无需回填。生成迁移文件不等于已执行数据库升级。

## NeuBell WebHook（WebAPI）通知设置

为纽铃（NeuBell）增加外部 WebHook（WebAPI）通知能力：管理员可添加一个或多个通知端点，当任一纽铃 Provider 的条目发生**新增（added）**或**移除（removed）**时，系统以**异步方式** POST 通知到匹配的地址，不阻塞业务请求与 Footer 刷新。

此外还支持**按调用触发**：在 FunctionRender「纽铃可见提醒测试」（`SystemInfoAppService.TriggerNeuBellTest`，action=`send`）中提供可选参数 **`WebHookUrl`（WebHook 地址，可选）** 与 **`WebHookMethod`（请求方式，可选）**——填写地址后，创建该条纽铃时会立即向该地址异步发送一条 `kind=item-created` 事件（fire-and-forget，不阻塞 Function 响应）；留空则只走常规监测分发。

每个 WebHook 端点支持**请求方式**（`GET` / `POST` / `PUT`，默认 `POST`）与**请求体模板**（可选，支持 `{{占位符}}`，与 Workflow 文本模板同格式）：地址与请求体中的占位符会在发送时替换为本次通知的实际数据；`GET` 请求不发送请求体（数据经地址占位符传递），签名仅对携带请求体的请求生效。

所有 WebHook 出站请求（`item-created` / `items-changed` / `test` 三类）的**完整请求报文与结果**都会记录到「请求日志」列表（管理页 NeuBell → 请求日志），支持查看报文、按条删除与批量清空（保留最近 50 条）。

管理入口：页脚“纽铃”抽屉 → “WebHook 设置”（页面 `Areas/Admin/Pages/NeuBell/Index`，仅超级管理员可访问）。

每条设置支持：

- **名称 / WebHook 地址**：仅允许 http/https 绝对地址（`NeuBellWebHook.TryValidateUrl` 校验）；地址可包含 `{{占位符}}`（保存时按掩码校验，发送前对渲染后的真实地址再次严格校验，渲染后不合法的地址只记失败日志、不发送）。
- **请求方式**：`GET` / `POST` / `PUT`（默认 `POST`）。`GET` 不发送请求体，模板设置不生效。
- **请求体模板（可选）**：留空使用系统默认的结构化 JSON 报文；模板（去除首尾空白后）以 `{` 或 `[` 开头时按 JSON 处理——字符串占位符自动做 JSON 转义后嵌入，`{{payload}}` 原样嵌入为 JSON 片段，`Content-Type` 为 `application/json`；否则按纯文本处理（`text/plain; charset=utf-8`）。
- **Provider 过滤**：留空通知全部 Provider；填写 ProviderId 只通知对应模块（如 `admin-neubell-test`）。
- **签名密钥（可选）**：配置后携带请求体的请求会附 `X-NeuBell-Signature: t=<unix>,v1=<hex>` 请求头，`v1 = HMAC-SHA256(secret, t + "." + body)`（签名覆盖实际发出的请求体字节），便于接收端验签。
- **新增时通知 / 移除时通知**：分别控制 added / removed 两类事件。
- **启用开关**：停用的设置不参与分发；管理页“测试”按钮会向该端点发送一条 `kind=test` 事件用于连通性验证。

通知报文（`application/json`）示例：

```json
{
  "source": "NeuBell",
  "kind": "items-changed",
  "providerId": "admin-neubell-test",
  "moduleUid": "Senparc.Areas.Admin",
  "displayName": "Admin 测试纽铃",
  "occurredAt": "2026-09-06T12:00:00+00:00",
  "added": [ { "id": "task-1", "title": "任务一", "summary": "摘要", "count": 1, "severity": "info", "detailUrl": "/Admin/...", "updatedAt": "2026-09-06T12:00:00+00:00" } ],
  "removed": []
}
```

`item-created`（创建时按参数触发）报文示例：

```json
{
  "source": "NeuBell",
  "kind": "item-created",
  "providerId": "admin-neubell-test",
  "occurredAt": "2026-09-11T12:00:00+00:00",
  "item": { "id": "function-reminder-1", "title": "任务一", "summary": "摘要", "link": "/Admin/Index", "status": "warning", "count": 1, "updated": "2026-09-11T12:00:00+00:00" }
}
```

### 请求方式与占位符（模板）

WebHook 地址与请求体模板支持 `{{name}}` 占位符（与 Workflow 文本模板同格式）。占位符替换规则：

- **URL**：替换值自动做 URL 编码（`Uri.EscapeDataString`）；
- **JSON 模板**：字符串占位符自动做 JSON 转义后嵌入（不额外加引号）；`{{payload}}` 的值原样嵌入为 JSON 片段；
- **纯文本模板**：原值替换；
- 未知占位符（合法命名但本次事件没有对应数据）替换为空字符串；不合法的占位符文本（如 `{{bad-name}}`）原样保留；
- 渲染只扫描原始模板文本，替换结果不会被二次扫描（无递归渲染）。

可用占位符：

| 占位符 | 含义 | 适用事件 |
| --- | --- | --- |
| `{{kind}}` | 事件类型（`item-created` / `items-changed` / `test`） | 全部 |
| `{{provider}}` | ProviderId（`test` 事件为 Provider 过滤值，可空） | 全部 |
| `{{providerName}}` | Provider 显示名称（`test` 事件为 WebHook 名称） | 全部 |
| `{{time}}` | 通知时间（ISO 8601） | 全部 |
| `{{payload}}` | 完整默认 JSON 报文 | 全部 |
| `{{id}}` | 条目 Id（`items-changed` 取第一条新增，无新增时取第一条移除） | item-created / items-changed |
| `{{title}}` | 条目标题 | item-created / items-changed |
| `{{summary}}` | 条目摘要 | item-created / items-changed |
| `{{link}}` | 条目链接；配置 `NeuBellWebHook:BaseUrl` 且链接为相对路径时拼成绝对链接 | item-created / items-changed |
| `{{status}}` | 条目严重程度（severity） | item-created / items-changed |
| `{{count}}` | 条目数量 | item-created / items-changed |
| `{{updated}}` | 条目更新时间（ISO 8601） | item-created / items-changed |
| `{{addedCount}}` / `{{removedCount}}` | 本次新增 / 移除数量 | items-changed |
| `{{addedTitles}}` / `{{removedTitles}}` | 本次新增 / 移除条目标题（以“、”连接） | items-changed |

示例（飞书/钉钉机器人风格的自定义请求体，`POST` + JSON）：

```json
{
  "text": "[NeuBell] {{title}}（{{status}}）
{{summary}}
查看详情：{{link}}",
  "event": "{{kind}}",
  "provider": "{{providerName}}"
}
```

示例（`GET` 回调，数据经地址占位符传递，不发送请求体与签名）：

```
https://example.com/api/neubell?kind={{kind}}&title={{title}}&link={{link}}
```

> `{{link}}` 绝对链接：`Senparc.Web/appsettings.json` 的 `NeuBellWebHook:BaseUrl`（可选）配置站点根地址（如 `https://admin.example.com`）后，形如 `/Admin/Index` 的相对链接会被拼成绝对链接；未配置时原样输出。

关键实现：

- `Domain/Models/DatabaseModel/NeuBellWebHookModels.cs`：实体 `NeuBellWebHook`（表 `ADMIN_NeuBellWebHook`），含 URL 校验与 Provider 匹配逻辑。
- `Domain/Services/NeuBellWebHookService.cs` / `ACL/Repository/NeuBellWebHookRepository.cs`：设置项 CRUD 与启用列表查询（契约 `INeuBellWebHookService` 便于测试替换）。
- `Domain/Services/NeuBellWebHookDispatcher.cs`：按 Provider 维护条目基线，diff 出 added/removed 后 fire-and-forget 分发（`IHttpClientFactory` 命名客户端 `NeuBellWebHook`，`SemaphoreSlim(4)` 限制并发出站）。
  - 首次观测只建基线，避免进程重启把存量条目误报为“新增”；
  - Provider 本轮缺席且仍在开放列表中（快照获取失败）时保留基线等待下轮，避免误报移除；模块被关闭时才将存量条目按“移除”通知。
- `Domain/Services/NeuBellWebHookLogService.cs` / `ACL/Repository/NeuBellWebHookRepository.cs`：请求日志实体 `NeuBellWebHookLog`（表 `ADMIN_NeuBellWebHookLog`）的「发送中 → 成功/失败」两段式记录；日志写入失败只记警告，绝不影响通知本身。
- 按调用触发：`OHS/Local/PL/NeuBellTest_Request.cs` 的 `WebHookUrl` / `WebHookMethod` 可选参数 → `SystemInfoAppService` 以 fire-and-forget 方式调用 `NeuBellWebHookDispatcher.NotifyItemCreatedAsync`（模板/渲染后地址无效均不发起 HTTP，但落一条失败日志便于排查）。
- `Domain/Services/NeuBellWebHookTemplate.cs`：占位符渲染器（URL 编码 / JSON 转义 / 纯文本三种模式），纯静态无状态，便于单测。
- `Domain/Services/NeuBellWebHookMonitorService.cs`：`IHostedService` 监测循环，轮询间隔由 `appsettings.json` 的 `NeuBellWebHook:PollingIntervalSeconds` 控制（默认 30 秒，最小 5 秒），并订阅 `NeuBellChangeNotifier` 变更事件提前唤醒一轮观测。
- 数据库迁移已同步生成至 Sqlite / SqlServer / MySql / Dm / Oracle / PostgreSQL 六个提供方（`Domain/Migrations/*`）。

相关单元测试见 `Tests/Senparc.Areas.Admin.Tests/Domain/Services/NeuBellWebHookDispatcherTests.cs`（基线建立、新增/移除检测、过滤匹配、缺席判定、HMAC 签名、测试事件，以及 `item-created` 按调用通知与请求日志的成功/失败记录）与 `NeuBellWebHookTemplateTests.cs`（URL 编码、JSON/文本模板模式、`{{payload}}` 原样嵌入、未知/非法占位符、请求方式规范化与模板地址校验）。
