# NcfSimulatedSite Aspire 使用说明（.NET 10）

本文档只针对 `tools/NcfSimulatedSite` 目录内项目。

## 1. 当前改造结果

### 1.1 Aspire 核心升级
- `Senparc.Aspire.AppHost` 已升级到 `net10.0`。
- AppHost 已接入 `Aspire.AppHost.Sdk 13.4.6` 与 `Aspire.Hosting.AppHost 13.4.6`。
- AppHost 资源编排包含：
  - `Senparc.Xncf.Installer`
  - `Senparc.Xncf.Accounts`
  - `Senparc.Web`
- 依赖关系：
  - `Senparc.Web` -> `Senparc.Xncf.Installer`
  - `Senparc.Web` -> `Senparc.Xncf.Accounts`
  - `Senparc.Xncf.Installer` -> `Senparc.Xncf.Accounts`

### 1.2 ServiceDefaults 升级
- `Senparc.Aspire.ServiceDefaults` 已升级到 `net10.0`。
- 统一默认能力已对齐新版依赖：
  - `Microsoft.Extensions.Http.Resilience 10.0.0`
  - `Microsoft.Extensions.ServiceDiscovery 10.0.0`
  - `OpenTelemetry.* 1.12.0`

### 1.3 业务服务接入状态
以下服务均已启用：
- `builder.AddServiceDefaults();`
- `app.MapDefaultEndpoints();`

涉及项目：
- `Senparc.Web`
- `Senparc.Xncf.Installer`
- `Senparc.Xncf.Accounts`

### 1.4 AppHost 启动配置
`launchSettings.json` 已切换到新版变量名：
- `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL`
- `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`

## 2. Aspire 在当前站点可直接使用的能力

### 2.1 统一可观测性（日志/指标/链路）
通过 ServiceDefaults 自动启用：
- ASP.NET Core 入站请求追踪
- HttpClient 出站请求追踪
- 运行时指标（runtime metrics）
- OTLP 导出（被 Aspire Dashboard 接收）

直接收益：
- 可以按服务看调用耗时与错误率。
- 可以按请求链路看从入口到下游调用的完整 Trace。
- 可以把异常日志与请求链路关联。

### 2.2 服务发现 + 默认弹性策略
通过 ServiceDefaults 自动启用：
- Service discovery
- HttpClient 标准 resilience handler

直接收益：
- 服务名路由更稳定。
- 下游偶发抖动时的容错能力更强。

### 2.3 健康检查端点
`MapDefaultEndpoints()` 在 Development 环境自动暴露：
- `/health`（就绪检查）
- `/alive`（存活检查）

直接收益：
- 可快速判断服务是否可接流量。
- 在 Dashboard/脚本里可统一探测服务健康度。

## 3. 如何启动与使用（本地）

## 3.1 前置条件
- .NET SDK 10.x
- 可访问 NuGet 源（首次还原 Aspire 13.x 必需）

## 3.2 建议启动方式
在仓库根目录执行：

```bash
dotnet restore tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
dotnet run --project tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
```

启动后会出现 Aspire Dashboard（端口由 launch profile 决定）。

## 3.3 Dashboard 常用查看顺序
1. Resources：先看所有服务是否都处于 Running。  
2. Traces：看慢请求、失败请求（5xx/4xx/异常）。  
3. Logs：按服务过滤，定位异常详情。  
4. Metrics：看请求量、失败率、延迟趋势。

## 4. 针对 AI / PromptRange 的调试说明

## 4.1 你最关心的“AI 请求异常返回”如何排查
典型场景：模型返回 429/500、超时、格式错误。

推荐路径：
1. 在 `Senparc.Web` 触发一次 AI 调用（页面或 API）。
2. 到 Dashboard -> Traces 找该请求。
3. 查看该 Trace 内的出站 HTTP Span：
   - 目标地址
   - 状态码
   - 耗时
   - 异常标记
4. 复制 TraceId，到 Dashboard Logs 过滤同一 TraceId。
5. 对照应用日志定位参数、业务分支、异常栈。

这能快速区分：
- 是模型服务返回异常（远端 4xx/5xx）
- 还是本地逻辑/序列化/鉴权问题

## 4.2 PromptRange 模块如何结合 Aspire 调试
`PromptRange` 在站点中通过项目引用集成在 `Senparc.Web` 内。

建议调试流程：
1. 用 AppHost 启动整站（而不是只单独跑 Web）。
2. 在 NCF 页面进入 PromptRange 功能并触发一次真实调用。
3. 在 Dashboard 的 `Senparc.Web` 资源下查看：
   - 入站请求 Trace
   - PromptRange 处理阶段日志
   - 出站 AI 调用 Span（若走 HttpClient）
4. 若涉及 `Installer/Accounts` 联动，继续在同一 Trace 中向下追踪。
5. 对比 `/health` 与 `/alive`，确认问题是否是实例不健康导致。

### 4.2.1 推荐重点观察项
- 首包延迟/总耗时是否异常升高
- 出站请求重试次数是否增加
- 失败是否集中在某个下游地址或某类模型
- 同一时间窗口内是否出现大面积 timeout

### 4.2.2 当 Trace 信息不足时
若 PromptRange 内部存在未通过 HttpClient 的关键调用，建议后续在模块内补充：
- `ActivitySource` 自定义 span
- 关键参数的结构化日志（避免敏感信息泄露）

这样可以把 Prompt 组装、路由选择、结果后处理也纳入同一链路。

## 5. Installer / Accounts 的联调点

### 5.1 已有测试端点（便于验证跨服务）
- `Senparc.Xncf.Installer`：
  - `/aspire-test1`（调用 Accounts）
  - `/aspire-test2`（调用 Installer 内部测试）

你可以用这两个端点快速验证：
- 服务发现是否生效
- 跨服务调用链路是否在 Dashboard 中串联

### 5.2 Accounts 服务
已接入 ServiceDefaults 与健康检查端点，适合单独观察：
- API 处理耗时
- 数据库访问带来的延迟变化

## 6. 常见问题与处理

### 6.1 `Aspire.AppHost.Sdk` 找不到
现象：
- `MSB4236` / 无法解析 `Aspire.AppHost.Sdk`

处理：
1. 确保 NuGet 源可访问（尤其是 `nuget.org`）。
2. 在联网环境执行一次 restore。
3. 再运行 AppHost。

### 6.2 构建警告（NU190x）
当前依赖里会出现一些安全公告警告（包含历史依赖树）。
不影响本次 Aspire 接入，但建议后续做一次统一依赖升级评估。

## 7. 与本次改造直接相关的文件

- `tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj`
- `tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Program.cs`
- `tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Properties/launchSettings.json`
- `tools/NcfSimulatedSite/Senparc.Aspire.ServiceDefaults/Senparc.Aspire.ServiceDefaults.csproj`
- `tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj`
- `tools/NcfSimulatedSite/Senparc.Web/Program.cs`
- `tools/NcfSimulatedSite/Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj`
- `tools/NcfSimulatedSite/Senparc.Xncf.Installer/Program.cs`
- `tools/NcfSimulatedSite/Senparc.Xncf.Accounts/Senparc.Xncf.Accounts.csproj`
- `tools/NcfSimulatedSite/Senparc.Xncf.Accounts/Program.cs`

# NcfSimulatedSite Aspire 使用说明（.NET 10）

本文档只针对 `tools/NcfSimulatedSite` 目录内项目。

## 1. 当前改造结果

### 1.1 Aspire 核心升级
- `Senparc.Aspire.AppHost` 已升级到 `net10.0`。
- AppHost 已接入 `Aspire.AppHost.Sdk 13.4.6` 与 `Aspire.Hosting.AppHost 13.4.6`。
- AppHost 资源编排包含：
  - `Senparc.Xncf.Installer`
  - `Senparc.Xncf.Accounts`
  - `Senparc.Web`
- 依赖关系：
  - `Senparc.Web` -> `Senparc.Xncf.Installer`
  - `Senparc.Web` -> `Senparc.Xncf.Accounts`
  - `Senparc.Xncf.Installer` -> `Senparc.Xncf.Accounts`

### 1.2 ServiceDefaults 升级
- `Senparc.Aspire.ServiceDefaults` 已升级到 `net10.0`。
- 统一默认能力已对齐新版依赖：
  - `Microsoft.Extensions.Http.Resilience 10.0.0`
  - `Microsoft.Extensions.ServiceDiscovery 10.0.0`
  - `OpenTelemetry.* 1.12.0`

### 1.3 业务服务接入状态
以下服务均已启用：
- `builder.AddServiceDefaults();`
- `app.MapDefaultEndpoints();`

涉及项目：
- `Senparc.Web`
- `Senparc.Xncf.Installer`
- `Senparc.Xncf.Accounts`

### 1.4 AppHost 启动配置
`launchSettings.json` 已切换到新版变量名：
- `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL`
- `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`

## 2. Aspire 在当前站点可直接使用的能力

### 2.1 统一可观测性（日志/指标/链路）
通过 ServiceDefaults 自动启用：
- ASP.NET Core 入站请求追踪
- HttpClient 出站请求追踪
- 运行时指标（runtime metrics）
- OTLP 导出（被 Aspire Dashboard 接收）

直接收益：
- 可以按服务看调用耗时与错误率。
- 可以按请求链路看从入口到下游调用的完整 Trace。
- 可以把异常日志与请求链路关联。

### 2.2 服务发现 + 默认弹性策略
通过 ServiceDefaults 自动启用：
- Service discovery
- HttpClient 标准 resilience handler

直接收益：
- 服务名路由更稳定。
- 下游偶发抖动时的容错能力更强。

### 2.3 健康检查端点
`MapDefaultEndpoints()` 在 Development 环境自动暴露：
- `/health`（就绪检查）
- `/alive`（存活检查）

直接收益：
- 可快速判断服务是否可接流量。
- 在 Dashboard/脚本里可统一探测服务健康度。

## 3. 如何启动与使用（本地）

## 3.1 前置条件
- .NET SDK 10.x
- 可访问 NuGet 源（首次还原 Aspire 13.x 必需）

## 3.2 建议启动方式
在仓库根目录执行：

```bash
dotnet restore tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
dotnet run --project tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj
```

启动后会出现 Aspire Dashboard（端口由 launch profile 决定）。

## 3.3 Dashboard 常用查看顺序
1. Resources：先看所有服务是否都处于 Running。  
2. Traces：看慢请求、失败请求（5xx/4xx/异常）。  
3. Logs：按服务过滤，定位异常详情。  
4. Metrics：看请求量、失败率、延迟趋势。

## 4. 针对 AI / PromptRange 的调试说明

## 4.1 你最关心的“AI 请求异常返回”如何排查
典型场景：模型返回 429/500、超时、格式错误。

推荐路径：
1. 在 `Senparc.Web` 触发一次 AI 调用（页面或 API）。
2. 到 Dashboard -> Traces 找该请求。
3. 查看该 Trace 内的出站 HTTP Span：
   - 目标地址
   - 状态码
   - 耗时
   - 异常标记
4. 复制 TraceId，到 Dashboard Logs 过滤同一 TraceId。
5. 对照应用日志定位参数、业务分支、异常栈。

这能快速区分：
- 是模型服务返回异常（远端 4xx/5xx）
- 还是本地逻辑/序列化/鉴权问题

## 4.2 PromptRange 模块如何结合 Aspire 调试
`PromptRange` 在站点中通过项目引用集成在 `Senparc.Web` 内。

建议调试流程：
1. 用 AppHost 启动整站（而不是只单独跑 Web）。
2. 在 NCF 页面进入 PromptRange 功能并触发一次真实调用。
3. 在 Dashboard 的 `Senparc.Web` 资源下查看：
   - 入站请求 Trace
   - PromptRange 处理阶段日志
   - 出站 AI 调用 Span（若走 HttpClient）
4. 若涉及 `Installer/Accounts` 联动，继续在同一 Trace 中向下追踪。
5. 对比 `/health` 与 `/alive`，确认问题是否是实例不健康导致。

### 4.2.1 推荐重点观察项
- 首包延迟/总耗时是否异常升高
- 出站请求重试次数是否增加
- 失败是否集中在某个下游地址或某类模型
- 同一时间窗口内是否出现大面积 timeout

### 4.2.2 当 Trace 信息不足时
若 PromptRange 内部存在未通过 HttpClient 的关键调用，建议后续在模块内补充：
- `ActivitySource` 自定义 span
- 关键参数的结构化日志（避免敏感信息泄露）

这样可以把 Prompt 组装、路由选择、结果后处理也纳入同一链路。

## 5. Installer / Accounts 的联调点

### 5.1 已有测试端点（便于验证跨服务）
- `Senparc.Xncf.Installer`：
  - `/aspire-test1`（调用 Accounts）
  - `/aspire-test2`（调用 Installer 内部测试）

你可以用这两个端点快速验证：
- 服务发现是否生效
- 跨服务调用链路是否在 Dashboard 中串联

### 5.2 Accounts 服务
已接入 ServiceDefaults 与健康检查端点，适合单独观察：
- API 处理耗时
- 数据库访问带来的延迟变化

## 6. 常见问题与处理

### 6.1 `Aspire.AppHost.Sdk` 找不到
现象：
- `MSB4236` / 无法解析 `Aspire.AppHost.Sdk`

处理：
1. 确保 NuGet 源可访问（尤其是 `nuget.org`）。
2. 在联网环境执行一次 restore。
3. 再运行 AppHost。

### 6.2 构建警告（NU190x）
当前依赖里会出现一些安全公告警告（包含历史依赖树）。
不影响本次 Aspire 接入，但建议后续做一次统一依赖升级评估。

## 7. 与本次改造直接相关的文件

- `tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Senparc.Aspire.AppHost.csproj`
- `tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Program.cs`
- `tools/NcfSimulatedSite/Senparc.Aspire.AppHost/Properties/launchSettings.json`
- `tools/NcfSimulatedSite/Senparc.Aspire.ServiceDefaults/Senparc.Aspire.ServiceDefaults.csproj`
- `tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj`
- `tools/NcfSimulatedSite/Senparc.Web/Program.cs`
- `tools/NcfSimulatedSite/Senparc.Xncf.Installer/Senparc.Xncf.Installer.csproj`
- `tools/NcfSimulatedSite/Senparc.Xncf.Installer/Program.cs`
- `tools/NcfSimulatedSite/Senparc.Xncf.Accounts/Senparc.Xncf.Accounts.csproj`
- `tools/NcfSimulatedSite/Senparc.Xncf.Accounts/Program.cs`

