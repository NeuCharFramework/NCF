## Senparc.Areas.Admin 项目说明

此项目是管理员后台模块，同样属于 XNCF 模块。不过这是一个特殊的模块，在系统安装的时候就被装载，并且可用于管理其他所有模块。

如果你狠狠心，也可以将此模块删除，这不会影响系统的正常运行，只不过你无法再进入管理后台。当然对黑客来说也一样。

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
