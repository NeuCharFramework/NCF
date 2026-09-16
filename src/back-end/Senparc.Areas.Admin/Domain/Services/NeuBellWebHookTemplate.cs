/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookTemplate.cs
    文件功能描述：NeuBell WebHook 模板渲染：
    将 WebHook 地址与请求体中的 {{占位符}}（与 Workflow 文本模板同格式）
    替换为本次通知的实际数据。URL 中的占位符替换值会自动做 URL 编码；
    请求体模板以 { 或 [ 开头时按 JSON 处理（字符串占位符自动 JSON 转义，
    {{payload}} 原样嵌入为 JSON 片段），否则按纯文本处理

    创建标识：Senparc - 20260914

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// NeuBell WebHook 模板渲染器（纯静态、无状态，供 Dispatcher 与校验逻辑使用）。
/// 占位符格式与 Workflow 文本模板一致：<c>{{name}}</c>，name 仅允许
/// <c>[A-Za-z][A-Za-z0-9_]*</c>。渲染时只扫描原始模板文本，
/// 替换结果不会被二次扫描（避免循环渲染）。
/// </summary>
public static class NeuBellWebHookTemplate
{
    private static readonly JsonSerializerOptions JsonStringOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 默认完整 JSON 报文对应的占位符（在请求体模板中原样嵌入为 JSON 片段）
    /// </summary>
    public const string TokenPayload = "payload";
    public const string TokenOperation = "operation";
    public const string TokenOperationStatus = "operationStatus";

    private static readonly string[] KnownTokens =
    {
        TokenPayload,
        "kind", "action", "actionName", "actionStatus",
        "provider", "providerName", "time",
        TokenOperation, TokenOperationStatus, "moduleUid", "tenantId",
        "id", "title", "summary", "link", "status", "count", "updated",
        "addedCount", "removedCount", "addedTitles", "removedTitles"
    };

    /// <summary>
    /// 已知占位符列表（供 UI 说明与文档展示）
    /// </summary>
    public static IReadOnlyList<string> KnownTokenList => KnownTokens;

    /// <summary>
    /// 将文本中的合法占位符替换为探测值“x”（用于含占位符的地址模板校验）
    /// </summary>
    public static string MaskTokens(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf("{{", StringComparison.Ordinal) < 0)
        {
            return text ?? string.Empty;
        }
        return ReplaceTokens(text, _ => "x");
    }

    /// <summary>
    /// 渲染 URL 模板：占位符替换值自动做 URL 编码（Uri.EscapeDataString）。
    /// 未知占位符替换为空字符串；不合法的占位符文本原样保留。
    /// </summary>
    public static string RenderUrl(string urlTemplate, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrEmpty(urlTemplate))
        {
            return urlTemplate ?? string.Empty;
        }
        return ReplaceTokens(urlTemplate, name =>
        {
            var value = Lookup(tokens, name) ?? string.Empty;
            return Uri.EscapeDataString(value);
        });
    }

    /// <summary>
    /// 渲染请求体模板。
    /// 模板（去除首尾空白后）以 <c>{</c> 或 <c>[</c> 开头时按 JSON 模式处理：
    /// 字符串占位符值自动做 JSON 转义后嵌入（不额外加引号），
    /// <see cref="TokenPayload"/> 占位符的值原样嵌入为 JSON 片段；
    /// 其余情况按纯文本模式处理（原值替换）。
    /// </summary>
    public static string RenderBody(string bodyTemplate, IReadOnlyDictionary<string, string> tokens, out string contentType)
    {
        if (string.IsNullOrEmpty(bodyTemplate))
        {
            contentType = null;
            return string.Empty;
        }

        var jsonMode = IsJsonTemplate(bodyTemplate);
        contentType = jsonMode ? "application/json" : "text/plain; charset=utf-8";
        return ReplaceTokens(bodyTemplate, name =>
        {
            var value = Lookup(tokens, name) ?? string.Empty;
            if (jsonMode && name != TokenPayload)
            {
                // 借助 JsonSerializer 做 JSON 字符串转义（取去掉首尾引号的部分）
                return JsonSerializer.Serialize(value, JsonStringOptions)[1..^1];
            }
            return value;
        });
    }

    /// <summary>
    /// 模板是否按 JSON 处理（去除首尾空白后以 { 或 [ 开头）
    /// </summary>
    public static bool IsJsonTemplate(string bodyTemplate)
    {
        var trimmed = (bodyTemplate ?? string.Empty).TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static string ReplaceTokens(
        string text,
        Func<string, string> replacement)
    {
        var result = new System.Text.StringBuilder(text.Length);
        var index = 0;
        while (index < text.Length)
        {
            var start = text.IndexOf("{{", index, StringComparison.Ordinal);
            if (start < 0)
            {
                result.Append(text, index, text.Length - index);
                break;
            }

            var nameStart = start + 2;
            var nameEnd = nameStart;
            while (nameEnd < text.Length && IsTokenNameChar(text[nameEnd]))
            {
                nameEnd++;
            }

            var name = text.Substring(nameStart, nameEnd - nameStart);
            if (nameEnd + 1 < text.Length && text[nameEnd] == '}' && text[nameEnd + 1] == '}' && IsTokenName(name))
            {
                result.Append(text, index, start - index);
                result.Append(replacement(name));
                index = nameEnd + 2;
                continue;
            }

            result.Append(text, index, start + 2 - index);
            index = start + 2;
        }
        return result.ToString();
    }

    private static bool IsTokenName(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsLetter(name[0]))
        {
            return false;
        }
        for (var i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsTokenNameChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static string Lookup(IReadOnlyDictionary<string, string> tokens, string name)
    {
        if (tokens == null)
        {
            return null;
        }
        foreach (var pair in tokens)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }
        return null;
    }
}
