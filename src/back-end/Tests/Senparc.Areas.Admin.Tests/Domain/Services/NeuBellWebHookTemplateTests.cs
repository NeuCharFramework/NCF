/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookTemplateTests.cs
    文件功能描述：NeuBell WebHook 模板渲染测试：
    URL 占位符编码、JSON/文本请求体模式、{{payload}} 原样嵌入、
    未知与非法占位符处理

    创建标识：Senparc - 20260914

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text.Json;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuBellWebHookTemplateTests
{
    private static Dictionary<string, string> NewTokens()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = "任务 一&二",
            ["status"] = "warning",
            ["link"] = "/Admin/Index?x=1",
            ["provider"] = "provider-a",
            ["payload"] = "{\"a\":1}",
            ["empty"] = ""
        };
    }

    [TestMethod]
    public void RenderUrl_ShouldPercentEncodeTokenValues()
    {
        var tokens = NewTokens();
        var url = NeuBellWebHookTemplate.RenderUrl(
            "https://example.com/hook/{{title}}/{{status}}", tokens);

        Assert.AreEqual("https://example.com/hook/%E4%BB%BB%E5%8A%A1%20%E4%B8%80%26%E4%BA%8C/warning", url);
    }

    [TestMethod]
    public void RenderUrl_ShouldReplaceUnknownTokenWithEmptyAndKeepInvalidTokenText()
    {
        var tokens = NewTokens();
        var url = NeuBellWebHookTemplate.RenderUrl(
            "https://example.com/hook/{{unknown}}/{{status}}?x={{bad-name}}", tokens);

        Assert.AreEqual("https://example.com/hook//warning?x={{bad-name}}", url);
    }

    [TestMethod]
    public void RenderUrl_ShouldNotRescanSubstitutedValues()
    {
        var tokens = NewTokens();
        tokens["title"] = "plain {{status}}";
        var url = NeuBellWebHookTemplate.RenderUrl("https://example.com/{{title}}", tokens);
        Assert.AreEqual("https://example.com/plain%20%7B%7Bstatus%7D%7D", url);
    }

    [TestMethod]
    public void RenderBody_JsonMode_ShouldEscapeStringTokensAndEmbedPayloadRaw()
    {
        var tokens = NewTokens();
        var template = "{\"text\": \"{{title}}\", \"raw\": {{payload}}}";
        var rendered = NeuBellWebHookTemplate.RenderBody(template, tokens, out var contentType);

        Assert.AreEqual("application/json", contentType);
        using var doc = JsonDocument.Parse(rendered);
        Assert.AreEqual("任务 一&二", doc.RootElement.GetProperty("text").GetString());
        Assert.AreEqual(1, doc.RootElement.GetProperty("raw").GetProperty("a").GetInt32());
        StringAssert.Contains(rendered, "任务 一&二");
        Assert.IsFalse(rendered.Contains("\\u4EFB", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RenderBody_JsonMode_ShouldEscapeQuotesNewlineBackslash()
    {
        var tokens = NewTokens();
        tokens["title"] = "a\"b\nc\\d";
        var rendered = NeuBellWebHookTemplate.RenderBody("{\"t\":\"{{title}}\"}", tokens, out _);

        using var doc = JsonDocument.Parse(rendered);
        Assert.AreEqual("a\"b\nc\\d", doc.RootElement.GetProperty("t").GetString());
    }

    [TestMethod]
    public void RenderBody_TextMode_ShouldSubstituteRawValues()
    {
        var tokens = NewTokens();
        var rendered = NeuBellWebHookTemplate.RenderBody(
            "NeuBell: {{title}} [{{status}}] {{unknown}}", tokens, out var contentType);

        Assert.AreEqual("text/plain; charset=utf-8", contentType);
        Assert.AreEqual("NeuBell: 任务 一&二 [warning] ", rendered);
    }

    [TestMethod]
    public void RenderBody_ArrayTemplate_ShouldBeTreatedAsJson()
    {
        var tokens = NewTokens();
        var rendered = NeuBellWebHookTemplate.RenderBody("{{payload}}", tokens, out var contentType);

        Assert.AreEqual("application/json", contentType);
        using var doc = JsonDocument.Parse(rendered);
        Assert.AreEqual(1, doc.RootElement.GetProperty("a").GetInt32());
    }

    [TestMethod]
    public void IsJsonTemplate_ShouldDetectByLeadingBraceOrBracket()
    {
        Assert.IsTrue(NeuBellWebHookTemplate.IsJsonTemplate("{\n  \"a\": 1\n}"));
        Assert.IsTrue(NeuBellWebHookTemplate.IsJsonTemplate("   [1,2]"));
        Assert.IsFalse(NeuBellWebHookTemplate.IsJsonTemplate("纯文本 {{title}}"));
        Assert.IsFalse(NeuBellWebHookTemplate.IsJsonTemplate(""));
        Assert.IsFalse(NeuBellWebHookTemplate.IsJsonTemplate(null));
    }

    [TestMethod]
    public void MaskTokens_ShouldReplaceValidTokensOnly()
    {
        var tokens = NewTokens();
        Assert.AreEqual(
            "https://example.com/hook/x/x?x={{bad-name}}",
            NeuBellWebHookTemplate.MaskTokens("https://example.com/hook/{{kind}}/{{title}}?x={{bad-name}}"));
        Assert.AreEqual(
            "no tokens",
            NeuBellWebHookTemplate.MaskTokens("no tokens"));
        Assert.AreEqual(
            string.Empty,
            NeuBellWebHookTemplate.MaskTokens(null));
    }

    [TestMethod]
    public void RenderBody_EmptyTemplate_ShouldReturnEmptyWithNullContentType()
    {
        var tokens = NewTokens();
        Assert.AreEqual(string.Empty, NeuBellWebHookTemplate.RenderBody(null, tokens, out var contentType));
        Assert.IsNull(contentType);
    }

    [TestMethod]
    public void RenderBody_ShouldSupportOperationStateTokens()
    {
        var tokens = NewTokens();
        tokens[NeuBellWebHookTemplate.TokenOperation] = "added";
        tokens[NeuBellWebHookTemplate.TokenOperationStatus] = "新增";

        var rendered = NeuBellWebHookTemplate.RenderBody(
            "{\"operation\":\"{{operation}}\",\"operationStatus\":\"{{operationStatus}}\"}",
            tokens,
            out _);

        using var doc = JsonDocument.Parse(rendered);
        Assert.AreEqual("added", doc.RootElement.GetProperty("operation").GetString());
        Assert.AreEqual("新增", doc.RootElement.GetProperty("operationStatus").GetString());
    }
}
