/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookModels.cs
    文件功能描述：NeuBell WebHook（WebAPI）设置与请求日志实体：
    为纽铃（NeuBell）提供外部 WebHook 通知能力，当纽铃条目新增（added）
    或移除（removed）时，按配置以异步方式请求对应的 WebHook 地址；
    同时记录每一次 WebHook 请求的完整数据与结果（NeuBellWebHookLog）

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260914
    修改描述：v0.7.1 WebHook 增强：请求方式（GET/POST/PUT）、请求体模板与 {{占位符}}
    （与 Workflow 文本模板同格式）、请求日志记录请求方式与渲染后的实际请求数据

    修改标识：Senparc - 20260915
    修改描述：v0.8.0 增强 Admin Chat Harness、轨迹回放与 NeuBell 管理能力

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel;

/// <summary>
/// 纽铃 WebHook 设置：一条记录对应一个外部 WebAPI（WebHook）通知端点。
/// 当某个纽铃 Provider 的条目发生新增或移除时，Dispatcher 会按
/// <see cref="ProviderFilter"/> 匹配并异步 POST 通知。
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuBellWebHook))]
[Serializable]
public class NeuBellWebHook : EntityBase<int>
{
    /// <summary>
    /// 显示名称
    /// </summary>
    [Required, MaxLength(200)]
    public string Name { get; private set; }

    /// <summary>
    /// WebHook（WebAPI）请求地址，仅允许 http/https
    /// </summary>
    [Required, MaxLength(1000)]
    public string WebHookUrl { get; private set; }

    /// <summary>
    /// 只通知指定纽铃 Provider（ProviderId）；留空表示全部 Provider
    /// </summary>
    [MaxLength(200)]
    public string ProviderFilter { get; private set; }

    /// <summary>
    /// 可选签名密钥：配置后请求会携带 X-NeuBell-Signature（HMAC-SHA256）请求头
    /// </summary>
    [MaxLength(300)]
    public string Secret { get; private set; }

    /// <summary>
    /// 请求方式：GET / POST / PUT（默认 POST）。GET 请求不发送请求体，
    /// 数据通过地址中的 {{占位符}} 传递；签名仅对携带请求体的请求生效。
    /// </summary>
    [Required, MaxLength(10)]
    public string HttpMethod { get; private set; }

    /// <summary>
    /// 可选请求体模板：支持 {{占位符}}（与 Workflow 文本模板同格式）。
    /// 留空时使用系统默认的结构化 JSON 报文；模板以 { 或 [ 开头时按 JSON 处理
    /// （字符串占位符自动做 JSON 转义，{{payload}} 原样嵌入），否则按纯文本处理。
    /// GET 请求不发送请求体，本设置不生效。
    /// </summary>
    public string BodyTemplate { get; private set; }

    /// <summary>
    /// 条目新增（added）时是否通知
    /// </summary>
    public bool NotifyOnAdd { get; private set; }

    /// <summary>
    /// 条目移除（removed）时是否通知
    /// </summary>
    public bool NotifyOnRemove { get; private set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; private set; }

    public int AdminUserId { get; private set; }

    private NeuBellWebHook() { }

    public NeuBellWebHook(string name, string webHookUrl, int adminUserId)
    {
        Name = name?.Trim() ?? string.Empty;
        WebHookUrl = webHookUrl?.Trim() ?? string.Empty;
        ProviderFilter = string.Empty;
        Secret = string.Empty;
        HttpMethod = MethodPost;
        BodyTemplate = string.Empty;
        NotifyOnAdd = true;
        NotifyOnRemove = true;
        IsEnabled = true;
        AdminUserId = adminUserId;
    }

    /// <summary>
    /// 支持的请求方式
    /// </summary>
    public const string MethodGet = "GET";
    public const string MethodPost = "POST";
    public const string MethodPut = "PUT";

    /// <summary>
    /// 请求体模板最大长度（字符）
    /// </summary>
    public const int MaxBodyTemplateLength = 20000;

    public void UpdateInfo(
        string name,
        string webHookUrl,
        string providerFilter,
        string secret,
        bool notifyOnAdd,
        bool notifyOnRemove,
        bool isEnabled,
        string httpMethod = null,
        string bodyTemplate = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name.Trim();
        }
        if (!string.IsNullOrWhiteSpace(webHookUrl))
        {
            WebHookUrl = webHookUrl.Trim();
        }
        ProviderFilter = (providerFilter ?? string.Empty).Trim();
        Secret = (secret ?? string.Empty).Trim();
        NotifyOnAdd = notifyOnAdd;
        NotifyOnRemove = notifyOnRemove;
        IsEnabled = isEnabled;
        if (httpMethod != null)
        {
            HttpMethod = NormalizeHttpMethod(httpMethod);
        }
        if (bodyTemplate != null)
        {
            BodyTemplate = bodyTemplate;
        }
        SetUpdateTime();
    }

    /// <summary>
    /// 规范化请求方式（不区分大小写；空值回退 POST；非法值回退 POST）
    /// </summary>
    public static string NormalizeHttpMethod(string input)
    {
        var value = (input ?? string.Empty).Trim().ToUpperInvariant();
        return value is MethodGet or MethodPut ? value : MethodPost;
    }

    /// <summary>
    /// 校验请求方式（返回用户可读的错误信息）
    /// </summary>
    public static bool TryValidateHttpMethod(string input, out string normalized, out string error)
    {
        var value = (input ?? string.Empty).Trim().ToUpperInvariant();
        normalized = value;
        if (value.Length == 0 || value is MethodGet or MethodPost or MethodPut)
        {
            normalized = NormalizeHttpMethod(input);
            error = null;
            return true;
        }
        error = $"请求方式必须是 GET、POST 或 PUT（当前为“{input?.Trim()}”）";
        return false;
    }

    /// <summary>
    /// 校验 WebHook 地址是否合法（仅允许 http/https，且必须可解析为绝对 URI）
    /// </summary>
    public static bool TryValidateUrl(string url, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "WebHook 地址不能为空";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "WebHook 地址必须是合法的 http/https 绝对地址";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 校验“可包含 {{占位符}}”的 WebHook 地址模板：
    /// 先将合法占位符替换为探测值再按绝对 http/https 地址校验；
    /// 发送前 Dispatcher 会对渲染后的真实地址再次校验。
    /// </summary>
    public static bool TryValidateUrlTemplate(string url, out string error)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "WebHook 地址不能为空";
            return false;
        }

        var masked = NeuBellWebHookTemplate.MaskTokens(url.Trim());
        return TryValidateUrl(masked, out error);
    }

    /// <summary>
    /// 该 WebHook 是否关注指定 Provider（ProviderFilter 留空表示全部）
    /// </summary>
    public bool MatchesProvider(string providerId)
    {
        return string.IsNullOrWhiteSpace(ProviderFilter)
               || string.Equals(ProviderFilter, providerId, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 纽铃 WebHook 请求日志：记录每一次发往 WebHook（WebAPI）的请求数据与结果，
/// 包含创建 NeuBell 时按参数触发的单次通知、设置页“测试”事件与后台监测的变更通知。
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(NeuBellWebHookLog))]
[Serializable]
public class NeuBellWebHookLog : EntityBase<int>
{
    public const string StatusSending = "sending";
    public const string StatusSuccess = "success";
    public const string StatusFailed = "failed";

    /// <summary>
    /// 请求类型：item-created（创建 NeuBell 时按参数通知）/ items-changed（监测到条目变化）/ test（测试事件）
    /// </summary>
    [Required, MaxLength(50)]
    public string EventKind { get; private set; }

    /// <summary>
    /// 实际使用的请求方式（GET / POST / PUT）
    /// </summary>
    [MaxLength(10)]
    public string HttpMethod { get; private set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    [Required, MaxLength(1000)]
    public string WebHookUrl { get; private set; }

    /// <summary>
    /// 相关纽铃 Provider（ProviderId）；无则为空
    /// </summary>
    [MaxLength(200)]
    public string ProviderId { get; private set; }

    /// <summary>
    /// 展示标题（例如条目标题或 WebHook 名称）
    /// </summary>
    [MaxLength(500)]
    public string Title { get; private set; }

    /// <summary>
    /// 完整请求报文（JSON）
    /// </summary>
    public string Payload { get; private set; }

    /// <summary>
    /// 执行状态：sending / success / failed
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; private set; }

    /// <summary>
    /// 请求结果（HTTP 状态、异常信息等）
    /// </summary>
    [MaxLength(2000)]
    public string Result { get; private set; }

    /// <summary>
    /// HTTP 状态码（未收到响应时为空）
    /// </summary>
    public int? StatusCode { get; private set; }

    /// <summary>
    /// 请求耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; private set; }

    /// <summary>
    /// 请求完成时间（未完成时为空）
    /// </summary>
    public DateTime? FinishTime { get; private set; }

    /// <summary>
    /// 触发该请求的管理员 Id（0 表示系统后台监测）
    /// </summary>
    public int AdminUserId { get; private set; }

    private NeuBellWebHookLog() { }

    public NeuBellWebHookLog(
        string eventKind,
        string httpMethod,
        string webHookUrl,
        string providerId,
        string title,
        string payload,
        int adminUserId)
    {
        EventKind = (eventKind ?? string.Empty).Trim();
        HttpMethod = httpMethod ?? string.Empty;
        WebHookUrl = (webHookUrl ?? string.Empty).Trim();
        ProviderId = (providerId ?? string.Empty).Trim();
        Title = (title ?? string.Empty).Trim();
        Payload = payload ?? string.Empty;
        AdminUserId = adminUserId;
        Status = StatusSending;
        Result = string.Empty;
    }

    /// <summary>
    /// 记录请求结果
    /// </summary>
    public void MarkFinished(bool success, string result, int? statusCode, long elapsedMilliseconds)
    {
        Status = success ? StatusSuccess : StatusFailed;
        Result = (result ?? string.Empty).Trim();
        if (Result.Length > 2000)
        {
            Result = Result[..2000];
        }
        StatusCode = statusCode;
        ElapsedMilliseconds = elapsedMilliseconds;
        FinishTime = DateTime.Now;
        SetUpdateTime();
    }
}
