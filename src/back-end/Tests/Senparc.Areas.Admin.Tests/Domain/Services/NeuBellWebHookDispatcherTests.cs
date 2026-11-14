/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuBellWebHookDispatcherTests.cs
    文件功能描述：NeuBell WebHook 分发器测试：
    基线建立、新增/移除检测、Provider 过滤、签名与测试事件

    创建标识：Senparc - 20260906

    修改标识：Senparc - 20260911
    修改描述：v0.7.1 新增 WebHook 请求日志（NeuBellWebHookLog）测试

    修改标识：Senparc - 20260914
    修改描述：v0.7.1 适配请求方式（GET/POST/PUT）、请求体模板与 {{占位符}} 渲染；
    新增 GET 无请求体/无签名、模板渲染、渲染后地址二次校验与日志成功状态回归测试

----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuBellWebHookDispatcherTests
{
    [TestMethod]
    public async Task ObserveAsync_FirstObservation_ShouldEstablishBaselineWithoutDispatch()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new()
        {
            CreateHook("hook-a", "https://example.com/hook-a")
        });
        var snapshots = new[] { Snapshot("provider-a", ("item-1", "任务一")) };

        var changes = await dispatcher.ObserveAsync(snapshots, new[] { "provider-a" });

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task ObserveAsync_AddedItems_ShouldDispatchOnlyToMatchingHooks()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all", secret: "secret-a"),
            CreateHook("hook-other", "https://example.com/hook-other", providerFilter: "provider-b"),
            CreateHook("hook-no-add", "https://example.com/hook-no-add", providerFilter: "provider-a", notifyOnAdd: false)
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a") }, new[] { "provider-a" });
        var changes = await dispatcher.ObserveAsync(
            new[] { Snapshot("provider-a", ("item-1", "任务一"), ("item-2", "任务二")) },
            new[] { "provider-a" });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(2, changes[0].Added.Count);
        await handler.WaitUntilAsync(1);

        // 只有“全部 Provider + 允许新增”的 hook-all 收到通知
        CollectionAssert.Contains(
            handler.Requests.Select(request => request.Url).ToList(),
            "https://example.com/hook-all");
        Assert.AreEqual(1, handler.Requests.Count);

        var captured = handler.Requests.Single();
        Assert.IsTrue(captured.Body.Contains("items-changed"));
        Assert.IsTrue(captured.Body.Contains("item-1"));
        Assert.IsTrue(captured.Body.Contains("item-2"));
        Assert.IsNotNull(captured.Signature, "配置了 Secret 的 WebHook 必须携带签名头");
    }

    [TestMethod]
    public async Task ObserveAsync_RemovedItems_ShouldDispatchRemovalNotification()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        var changes = await dispatcher.ObserveAsync(new[] { Snapshot("provider-a") }, new[] { "provider-a" });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(1, changes[0].Removed.Count);
        Assert.AreEqual("item-1", changes[0].Removed[0].Id);
        await handler.WaitUntilAsync(1);
        Assert.IsTrue(handler.Requests.Single().Body.Contains("\"item-1\""));
    }

    [TestMethod]
    public async Task ObserveAsync_NoChange_ShouldNotDispatch()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        var changes = await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task ObserveAsync_ExpectedProviderMissingThisRound_ShouldKeepBaseline()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        // 本轮快照获取失败（provider 仍在 expected 中）：保留基线，不误报移除
        var changes = await dispatcher.ObserveAsync(Array.Empty<NeuBellSnapshot>(), new[] { "provider-a" });

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);

        // 恢复后只把真正的差集报为新增
        var recovered = await dispatcher.ObserveAsync(
            new[] { Snapshot("provider-a", ("item-1", "任务一"), ("item-2", "任务二")) },
            new[] { "provider-a" });
        CollectionAssert.AreEquivalent(
            new[] { "item-2" },
            recovered.SelectMany(change => change.Added).Select(item => item.Id).ToList());
    }

    [TestMethod]
    public async Task ObserveAsync_ProviderNoLongerExpected_ShouldFireRemoval()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });
        // 模块被关闭（不在 expected 中）：存量条目按移除通知
        var changes = await dispatcher.ObserveAsync(Array.Empty<NeuBellSnapshot>(), Array.Empty<string>());

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(1, changes[0].Removed.Count);
        await handler.WaitUntilAsync(1);
        Assert.IsTrue(handler.Requests.Single().Body.Contains("\"item-1\""));
    }

    [TestMethod]
    public async Task ObserveAsync_NullExpectedProviders_ShouldNotJudgeRemoval()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new()
        {
            CreateHook("hook-all", "https://example.com/hook-all")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a", ("item-1", "任务一")) });
        // expectedProviderIds 为 null（无法判定开放状态）：缺席一律不做移除判断
        var changes = await dispatcher.ObserveAsync(Array.Empty<NeuBellSnapshot>());

        Assert.AreEqual(0, changes.Count);
        await Task.Delay(200);
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public async Task SendTestAsync_ShouldPostTestEventWithoutItems()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new());
        var webHook = CreateHook("hook-all", "https://example.com/hook-test", providerFilter: "provider-a", secret: "s3cr3t");

        var (success, message) = await dispatcher.SendTestAsync(webHook);

        Assert.IsTrue(success, message);
        Assert.AreEqual(1, handler.Requests.Count);
        var captured = handler.Requests.Single();
        Assert.AreEqual("https://example.com/hook-test", captured.Url);
        Assert.IsTrue(captured.Body.Contains("\"test\""));
        Assert.IsFalse(captured.Body.Contains("items-changed"));
        Assert.IsNotNull(captured.Signature);
    }

    [TestMethod]
    public async Task SendTestAsync_InvalidUrl_ShouldFailWithoutRequest()
    {
        var (dispatcher, handler, _, _) = CreateDispatcher(new());
        var webHook = CreateHook("bad", "ftp://example.com/hook");

        var (success, message) = await dispatcher.SendTestAsync(webHook);

        Assert.IsFalse(success);
        Assert.IsFalse(string.IsNullOrEmpty(message));
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public void SignPayload_ShouldProduceTimestampAndHmacSha256()
    {
        const string payload = "{\"source\":\"NeuBell\"}";
        const string secret = "unit-test-secret";

        var signature = NeuBellWebHookDispatcher.SignPayload(secret, payload);

        Match match = Regex.Match(
            signature, @"^t=(\d+),v1=([0-9a-f]{64})$");
        Assert.IsTrue(match.Success, $"签名格式不符合 t=<unix>,v1=<hex>：{signature}");

        var expected = Encoding.UTF8.GetBytes(match.Groups[1].Value + "." + payload);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedHex = Convert.ToHexString(hmac.ComputeHash(expected)).ToLowerInvariant();
        Assert.AreEqual(expectedHex, match.Groups[2].Value, ignoreCase: true);
    }

    [TestMethod]
    public void BuildPayload_TestEvent_ShouldUseTestKindAndEmptyLists()
    {
        var payload = NeuBellWebHookDispatcher.BuildPayload(new NeuBellWebHookChange
        {
            ProviderId = "provider-a",
            ModuleUid = "module-a",
            DisplayName = "测试",
            OccurredAt = DateTimeOffset.UtcNow,
            Added = new[] { new NeuBellItem("item-1", "任务一", "摘要", 1, "info", "/detail", DateTimeOffset.UtcNow) },
            Removed = Array.Empty<NeuBellItem>()
        }, test: true);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.AreEqual("NeuBell", root.GetProperty("source").GetString());
        Assert.AreEqual("test", root.GetProperty("kind").GetString());
        Assert.AreEqual("test", root.GetProperty("action").GetString());
        Assert.AreEqual("success", root.GetProperty("actionStatus").GetString());
        Assert.AreEqual("test", root.GetProperty("operation").GetString());
        Assert.AreEqual("测试", root.GetProperty("operationStatus").GetString());
        Assert.AreEqual(0, root.GetProperty("added").GetArrayLength());
        Assert.AreEqual(0, root.GetProperty("removed").GetArrayLength());
    }

    [TestMethod]
    public void TryValidateUrl_ShouldAcceptHttpAndHttpsOnly()
    {
        Assert.IsTrue(NeuBellWebHook.TryValidateUrl("https://example.com/api/hook", out _));
        Assert.IsTrue(NeuBellWebHook.TryValidateUrl("http://127.0.0.1:5000/hook", out _));

        Assert.IsFalse(NeuBellWebHook.TryValidateUrl("ftp://example.com/hook", out var ftpError));
        StringAssert.Contains(ftpError, "http/https");
        Assert.IsFalse(NeuBellWebHook.TryValidateUrl("/relative/path", out _));
        Assert.IsFalse(NeuBellWebHook.TryValidateUrl("  ", out _));
        Assert.IsFalse(NeuBellWebHook.TryValidateUrl(null, out _));
    }

    [TestMethod]
    public void MatchesProvider_ShouldMatchEmptyFilterToAll()
    {
        var allHooks = CreateHook("all", "https://example.com/hook");
        var filtered = CreateHook("filtered", "https://example.com/hook", providerFilter: "provider-a");

        Assert.IsTrue(allHooks.MatchesProvider("provider-a"));
        Assert.IsTrue(allHooks.MatchesProvider("provider-z"));
        Assert.IsTrue(filtered.MatchesProvider("provider-a"));
        Assert.IsTrue(filtered.MatchesProvider("PROVIDER-A"));
        Assert.IsFalse(filtered.MatchesProvider("provider-b"));
    }

    [TestMethod]
    public async Task NotifyItemCreatedAsync_ValidUrl_ShouldSendRequestAndLogSuccess()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new());

        var task = dispatcher.NotifyItemCreatedAsync(
            "https://example.com/item-hook",
            "POST",
            new NeuBellItem("item-1", "任务一", "这是任务一的摘要", 1, "info", "/detail", DateTimeOffset.UtcNow),
            "provider-a",
            adminUserId: 7);

        // 快速落日志：调用方无需等待 HTTP 即可完成
        Assert.IsTrue(await logFake.WaitUntilPendingAsync(1), "应快速创建“发送中”日志");
        var pending = logFake.Pending.Single();
        Assert.AreEqual(NeuBellWebHookDispatcher.EventKindItemCreated, pending.EventKind);
        Assert.AreEqual("https://example.com/item-hook", pending.WebHookUrl);
        Assert.AreEqual("任务一", pending.Title);
        Assert.AreEqual(7, pending.AdminUserId);
        // 报文为 JSON（非 ASCII 可能被 \uXXXX 转义），按 JSON 语义校验
        using (var pendingDoc = JsonDocument.Parse(pending.Payload))
        {
            Assert.AreEqual("NeuBell", pendingDoc.RootElement.GetProperty("source").GetString());
            Assert.AreEqual(NeuBellWebHookDispatcher.EventKindItemCreated,
                pendingDoc.RootElement.GetProperty("kind").GetString());
            Assert.AreEqual("created", pendingDoc.RootElement.GetProperty("operation").GetString());
            Assert.AreEqual("创建", pendingDoc.RootElement.GetProperty("operationStatus").GetString());
            Assert.AreEqual("provider-a", pendingDoc.RootElement.GetProperty("providerId").GetString());
            Assert.AreEqual("任务一", pendingDoc.RootElement.GetProperty("item").GetProperty("title").GetString());
            Assert.AreEqual("这是任务一的摘要",
                pendingDoc.RootElement.GetProperty("item").GetProperty("summary").GetString());
        }
        StringAssert.Contains(pending.Payload, "任务一");
        Assert.IsFalse(pending.Payload.Contains("\\u4EFB", StringComparison.OrdinalIgnoreCase));

        var (queued, message) = await task;
        Assert.IsTrue(queued);
        Assert.AreEqual(1, handler.Requests.Count);
        var captured = handler.Requests.Single();
        Assert.AreEqual("https://example.com/item-hook", captured.Url);
        Assert.AreEqual("POST", captured.Method);
        Assert.IsTrue(captured.Body.Contains("provider-a"));
        using (var bodyDoc = JsonDocument.Parse(captured.Body))
        {
            Assert.AreEqual(NeuBellWebHookDispatcher.EventKindItemCreated,
                bodyDoc.RootElement.GetProperty("kind").GetString());
            Assert.AreEqual("任务一", bodyDoc.RootElement.GetProperty("item").GetProperty("title").GetString());
            Assert.AreEqual("item-1", bodyDoc.RootElement.GetProperty("item").GetProperty("id").GetString());
            Assert.AreEqual("info", bodyDoc.RootElement.GetProperty("item").GetProperty("status").GetString());
        }

        Assert.IsTrue(await logFake.WaitUntilCompletedAsync(1), "请求完成后应更新日志");
        var completed = logFake.Completed.Single();
        Assert.AreEqual(pending.Id, completed.LogId);
        Assert.IsTrue(completed.Success);
        Assert.AreEqual(200, completed.StatusCode);
        Assert.IsTrue(completed.ElapsedMilliseconds >= 0);
        _ = message;
    }

    [TestMethod]
    public async Task NotifyItemCreatedAsync_InvalidUrl_ShouldLogFailureWithoutHttp()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new());

        var (queued, message) = await dispatcher.NotifyItemCreatedAsync(
            "not-a-url",
            "POST",
            new NeuBellItem("item-1", "任务一", "摘要", 1, "info", "/detail", DateTimeOffset.UtcNow),
            "provider-a",
            adminUserId: 0);

        Assert.IsFalse(queued);
        Assert.IsFalse(string.IsNullOrEmpty(message));
        Assert.AreEqual(0, handler.Requests.Count, "无效地址不应发起 HTTP 请求");
        Assert.IsTrue(await logFake.WaitUntilCompletedAsync(1), "无效地址也应记录失败日志");
        var completed = logFake.Completed.Single();
        Assert.IsFalse(completed.Success);
        var failedPending = logFake.Pending.Single();
        Assert.AreEqual(NeuBellWebHookDispatcher.EventKindItemCreated, failedPending.EventKind);
        using var failedDoc = JsonDocument.Parse(failedPending.Payload);
        Assert.AreEqual("任务一", failedDoc.RootElement.GetProperty("item").GetProperty("title").GetString());
    }

    [TestMethod]
    public async Task SendTestAsync_ShouldLogTestKindWithRequestResult()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new());
        var webHook = CreateHook("hook-a", "https://example.com/hook-a");

        var (success, _) = await dispatcher.SendTestAsync(webHook, adminUserId: 3);

        Assert.IsTrue(success);
        Assert.AreEqual(1, handler.Requests.Count);
        using (var bodyDoc = JsonDocument.Parse(handler.Requests.Single().Body))
        {
            Assert.AreEqual(NeuBellWebHookDispatcher.EventKindTest,
                bodyDoc.RootElement.GetProperty("kind").GetString());
        }
        Assert.IsTrue(await logFake.WaitUntilCompletedAsync(1));
        var testPending = logFake.Pending.Single();
        Assert.AreEqual(NeuBellWebHookDispatcher.EventKindTest, testPending.EventKind);
        using (var testDoc = JsonDocument.Parse(testPending.Payload))
        {
            Assert.AreEqual(NeuBellWebHookDispatcher.EventKindTest,
                testDoc.RootElement.GetProperty("kind").GetString());
        }
        Assert.IsTrue(logFake.Completed.Single().Success);
    }

    [TestMethod]
    public async Task NotifyOperationAsync_ConsumeAction_ShouldSendRemovedItemsAndActionState()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new());
        var removed = new NeuBellItem(
            "item-consumed",
            "已消费提醒",
            "消费摘要",
            1,
            "warning",
            "/detail",
            DateTimeOffset.UtcNow);

        var (queued, message) = await dispatcher.NotifyOperationAsync(
            "https://example.com/consume-hook",
            "POST",
            new NeuBellWebHookOperation
            {
                EventKind = NeuBellWebHookDispatcher.EventKindItemsChanged,
                Action = "consume-one",
                ActionName = "消费最新一条",
                ActionStatus = "success",
                ProviderId = "admin-neubell-test",
                ProviderName = "NeuBell 测试",
                Removed = new[] { removed }
            },
            adminUserId: 7,
            bodyTemplate: "{\n  \"action\": \"{{action}}\",\n  \"actionStatus\": \"{{actionStatus}}\",\n  \"operationStatus\": \"{{operationStatus}}\",\n  \"title\": \"{{title}}\"\n}");

        Assert.IsTrue(queued, message);
        Assert.AreEqual(1, handler.Requests.Count);
        var captured = handler.Requests.Single();
        using var bodyDoc = JsonDocument.Parse(captured.Body);
        Assert.AreEqual("consume-one", bodyDoc.RootElement.GetProperty("action").GetString());
        Assert.AreEqual("success", bodyDoc.RootElement.GetProperty("actionStatus").GetString());
        Assert.AreEqual("移除", bodyDoc.RootElement.GetProperty("operationStatus").GetString());
        Assert.AreEqual("已消费提醒", bodyDoc.RootElement.GetProperty("title").GetString());
        StringAssert.Contains(captured.Body, "移除");
        Assert.IsTrue(await logFake.WaitUntilCompletedAsync(1));
        Assert.IsTrue(logFake.Completed.Single().Success);
    }

    [TestMethod]
    public async Task SendTestAsync_GetMethod_ShouldNotSendBodyOrSignature()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new());
        var webHook = CreateHook(
            "hook-get",
            "https://example.com/hook?kind={{kind}}&provider={{provider}}",
            providerFilter: "provider-a",
            secret: "s3cr3t",
            httpMethod: "GET");

        var (success, message) = await dispatcher.SendTestAsync(webHook, adminUserId: 3);

        Assert.IsTrue(success, message);
        Assert.AreEqual(1, handler.Requests.Count);
        var captured = handler.Requests.Single();
        Assert.AreEqual("GET", captured.Method);
        Assert.AreEqual(string.Empty, captured.Body, "GET 不应携带请求体");
        Assert.IsNull(captured.ContentType, "GET 不应携带请求体");
        Assert.IsNull(captured.Signature, "无请求体的请求不应携带签名");
        Assert.AreEqual("https://example.com/hook?kind=test&provider=provider-a", captured.Url);

        var pending = logFake.Pending.Single();
        Assert.AreEqual("GET", pending.HttpMethod);
        Assert.AreEqual(string.Empty, pending.Payload, "GET 请求日志的报文应为空字符串");
        Assert.IsTrue(logFake.Completed.Single().Success);
    }

    [TestMethod]
    public async Task ObserveAsync_CustomBodyTemplate_ShouldRenderTemplateAndUseJsonContentType()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new()
        {
            CreateHook(
                "hook-tmpl",
                "https://example.com/hook-tmpl",
                httpMethod: "POST",
                bodyTemplate: "{\"text\": \"[NeuBell] {{title}}（{{status}}）\"}")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a") }, new[] { "provider-a" });
        await dispatcher.ObserveAsync(
            new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });

        await handler.WaitUntilAsync(1);
        var captured = handler.Requests.Single();
        Assert.AreEqual("POST", captured.Method);
        Assert.AreEqual("application/json", captured.ContentType);
        using var bodyDoc = JsonDocument.Parse(captured.Body);
        Assert.AreEqual("[NeuBell] 任务一（info）", bodyDoc.RootElement.GetProperty("text").GetString());

        var pending = logFake.Pending.Single();
        Assert.AreEqual("POST", pending.HttpMethod);
        using var logDoc = JsonDocument.Parse(pending.Payload);
        Assert.AreEqual("[NeuBell] 任务一（info）", logDoc.RootElement.GetProperty("text").GetString());
        // 回归：成功发送后日志应标记成功，而不是“请求未完成”
        Assert.IsTrue(await logFake.WaitUntilCompletedAsync(1));
        var completed = logFake.Completed.Single();
        Assert.IsTrue(completed.Success, $"成功发送应记录为 success（实际：{completed.StatusCode}）");
    }

    [TestMethod]
    public async Task ObserveAsync_TemplatedUrl_ShouldRenderPercentEncodedUrl()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new()
        {
            CreateHook("hook-url-tmpl", "https://example.com/hook/{{kind}}/{{title}}")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a") }, new[] { "provider-a" });
        await dispatcher.ObserveAsync(
            new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });

        await handler.WaitUntilAsync(1);
        var captured = handler.Requests.Single();
        Assert.AreEqual("https://example.com/hook/items-changed/%E4%BB%BB%E5%8A%A1%E4%B8%80", captured.Url);
        var pending = logFake.Pending.Single();
        Assert.AreEqual("https://example.com/hook/items-changed/%E4%BB%BB%E5%8A%A1%E4%B8%80", pending.WebHookUrl);
    }

    [TestMethod]
    public async Task ObserveAsync_InvalidRenderedUrl_ShouldLogFailureWithoutHttp()
    {
        // 模板本身合法（占位符被掩码为 x 后可解析），但渲染后成为无主机的非法地址
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new()
        {
            CreateHook("hook-bad-render", "https://{{unknownToken}}/hook")
        });

        await dispatcher.ObserveAsync(new[] { Snapshot("provider-a") }, new[] { "provider-a" });
        await dispatcher.ObserveAsync(
            new[] { Snapshot("provider-a", ("item-1", "任务一")) }, new[] { "provider-a" });

        Assert.IsTrue(await logFake.WaitUntilCompletedAsync(1), "渲染后地址无效应记录失败日志");
        var completed = logFake.Completed.Single();
        Assert.IsFalse(completed.Success);
        Assert.AreEqual(0, handler.Requests.Count, "渲染后地址无效不应发起 HTTP 请求");
    }

    [TestMethod]
    public async Task NotifyItemCreatedAsync_TemplatedUrl_ShouldRenderItemTokens()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new());

        var (queued, _) = await dispatcher.NotifyItemCreatedAsync(
            "https://example.com/hook?title={{title}}&status={{status}}",
            "GET",
            new NeuBellItem("item-9", "任务九", "摘要", 3, "warning", "/Admin/Index", DateTimeOffset.UtcNow),
            "NeuBell 测试",
            adminUserId: 5);

        Assert.IsTrue(queued);
        Assert.AreEqual(1, handler.Requests.Count);
        var captured = handler.Requests.Single();
        Assert.AreEqual("GET", captured.Method);
        Assert.AreEqual("https://example.com/hook?title=%E4%BB%BB%E5%8A%A1%E4%B9%9D&status=warning", captured.Url);
        Assert.AreEqual(string.Empty, captured.Body);
        Assert.IsNull(captured.Signature);
        var pending = logFake.Pending.Single();
        Assert.AreEqual("GET", pending.HttpMethod);
        Assert.AreEqual(5, pending.AdminUserId);
        Assert.IsTrue(logFake.Completed.Single().Success);
    }

    [TestMethod]
    public async Task NotifyItemCreatedAsync_CustomBodyTemplate_ShouldRenderMultilineBodyAndOperationTokens()
    {
        var (dispatcher, handler, _, logFake) = CreateDispatcher(new());

        var (queued, message) = await dispatcher.NotifyItemCreatedAsync(
            "https://example.com/item-hook",
            "POST",
            new NeuBellItem("item-10", "任务十", "摘要", 1, "warning", "/detail", DateTimeOffset.UtcNow),
            "NeuBell 测试",
            adminUserId: 5,
            bodyTemplate: "{\n  \"title\": \"{{title}}\",\n  \"operation\": \"{{operation}}\",\n  \"operationStatus\": \"{{operationStatus}}\",\n  \"payload\": {{payload}}\n}");

        Assert.IsTrue(queued, message);
        Assert.AreEqual(1, handler.Requests.Count);
        var captured = handler.Requests.Single();
        Assert.AreEqual("POST", captured.Method);
        Assert.AreEqual("application/json", captured.ContentType);
        using var bodyDoc = JsonDocument.Parse(captured.Body);
        Assert.AreEqual("任务十", bodyDoc.RootElement.GetProperty("title").GetString());
        Assert.AreEqual("created", bodyDoc.RootElement.GetProperty("operation").GetString());
        Assert.AreEqual("创建", bodyDoc.RootElement.GetProperty("operationStatus").GetString());
        Assert.AreEqual("item-created", bodyDoc.RootElement.GetProperty("payload").GetProperty("kind").GetString());
        Assert.IsTrue(await logFake.WaitUntilCompletedAsync(1));
        Assert.IsTrue(logFake.Completed.Single().Success);
    }

    [TestMethod]
    public void TryValidateHttpMethod_ShouldNormalizeAndRejectInvalid()
    {
        Assert.IsTrue(NeuBellWebHook.TryValidateHttpMethod("post", out var normalized, out var error));
        Assert.AreEqual(NeuBellWebHook.MethodPost, normalized);
        Assert.IsNull(error);

        Assert.IsTrue(NeuBellWebHook.TryValidateHttpMethod(" get ", out normalized, out _));
        Assert.AreEqual(NeuBellWebHook.MethodGet, normalized);

        Assert.IsTrue(NeuBellWebHook.TryValidateHttpMethod(null, out normalized, out _));
        Assert.AreEqual(NeuBellWebHook.MethodPost, normalized, "空值应回退 POST");

        Assert.IsTrue(NeuBellWebHook.TryValidateHttpMethod("", out normalized, out _));
        Assert.AreEqual(NeuBellWebHook.MethodPost, normalized);

        Assert.IsFalse(NeuBellWebHook.TryValidateHttpMethod("DELETE", out _, out error));
        StringAssert.Contains(error, "GET、POST 或 PUT");

        Assert.AreEqual(NeuBellWebHook.MethodPost, NeuBellWebHook.NormalizeHttpMethod("bogus"));
        Assert.AreEqual(NeuBellWebHook.MethodPut, NeuBellWebHook.NormalizeHttpMethod("put"));
    }

    [TestMethod]
    public void TryValidateUrlTemplate_ShouldAcceptPlaceholdersAndRejectInvalidScheme()
    {
        Assert.IsTrue(NeuBellWebHook.TryValidateUrlTemplate("https://example.com/hook/{{kind}}/{{title}}", out _));
        Assert.IsTrue(NeuBellWebHook.TryValidateUrlTemplate("https://example.com/hook?provider={{provider}}", out _));
        Assert.IsFalse(NeuBellWebHook.TryValidateUrlTemplate("ftp://example.com/hook/{{kind}}", out var ftpError));
        StringAssert.Contains(ftpError, "http/https");
        Assert.IsFalse(NeuBellWebHook.TryValidateUrlTemplate("   ", out _));
    }

    private static NeuBellWebHook CreateHook(
        string name,
        string url,
        string providerFilter = null,
        string secret = null,
        bool notifyOnAdd = true,
        bool notifyOnRemove = true,
        string httpMethod = null,
        string bodyTemplate = null)
    {
        var webHook = new NeuBellWebHook(name, url, adminUserId: 1);
        webHook.UpdateInfo(
            name,
            url,
            providerFilter ?? string.Empty,
            secret ?? string.Empty,
            notifyOnAdd,
            notifyOnRemove,
            isEnabled: true,
            httpMethod,
            bodyTemplate);
        return webHook;
    }

    private static NeuBellSnapshot Snapshot(string providerId, params (string id, string title)[] items)
    {
        return new NeuBellSnapshot(
            providerId,
            "module-test",
            providerId,
            "fa fa-bell",
            true,
            items
                .Select(item => new NeuBellItem(item.id, item.title, "摘要", 1, "info", "/detail", DateTimeOffset.UtcNow))
                .ToList());
    }

    private static (NeuBellWebHookDispatcher dispatcher, CaptureHandler handler, FakeNeuBellWebHookService fake,
            FakeNeuBellWebHookLogService logFake)
        CreateDispatcher(List<NeuBellWebHook> webHooks)
    {
        var handler = new CaptureHandler();
        var fake = new FakeNeuBellWebHookService(webHooks);
        var logFake = new FakeNeuBellWebHookLogService();
        var services = new ServiceCollection();
        services.AddScoped<INeuBellWebHookService>(_ => fake);
        services.AddScoped<INeuBellWebHookLogService>(_ => logFake);
        var provider = services.BuildServiceProvider();
        var dispatcher = new NeuBellWebHookDispatcher(
            provider,
            new FakeHttpClientFactory(handler),
            NullLogger<NeuBellWebHookDispatcher>.Instance);
        return (dispatcher, handler, fake, logFake);
    }

    private sealed class FakeNeuBellWebHookLogService : INeuBellWebHookLogService
    {
        public sealed class PendingLog
        {
            public int Id { get; init; }
            public string EventKind { get; init; }
            public string HttpMethod { get; init; }
            public string WebHookUrl { get; init; }
            public string Title { get; init; }
            public string Payload { get; init; }
            public int AdminUserId { get; init; }
        }

        public sealed class CompletedLog
        {
            public int LogId { get; init; }
            public bool Success { get; init; }
            public int? StatusCode { get; init; }
            public long ElapsedMilliseconds { get; init; }
        }

        private int _nextId;

        public List<PendingLog> Pending { get; } = new();
        public List<CompletedLog> Completed { get; } = new();

        public Task<NeuBellWebHookLog> CreatePendingAsync(
            string eventKind,
            string httpMethod,
            string webHookUrl,
            string providerId,
            string title,
            string payload,
            int adminUserId)
        {
            var log = new NeuBellWebHookLog(eventKind, httpMethod, webHookUrl, providerId, title, payload, adminUserId);
            log.Id = Interlocked.Increment(ref _nextId);
            lock (Pending)
            {
                Pending.Add(new PendingLog
                {
                    Id = log.Id,
                    EventKind = eventKind,
                    HttpMethod = httpMethod,
                    WebHookUrl = webHookUrl,
                    Title = title,
                    Payload = payload,
                    AdminUserId = adminUserId
                });
            }
            return Task.FromResult(log);
        }

        public Task CompleteAsync(
            int logId,
            bool success,
            string result,
            int? statusCode,
            long elapsedMilliseconds)
        {
            lock (Completed)
            {
                Completed.Add(new CompletedLog
                {
                    LogId = logId,
                    Success = success,
                    StatusCode = statusCode,
                    ElapsedMilliseconds = elapsedMilliseconds
                });
            }
            return Task.CompletedTask;
        }

        public async Task<bool> WaitUntilPendingAsync(int expectedCount, int timeoutMilliseconds = 5000)
        {
            var start = Environment.TickCount64;
            while (true)
            {
                lock (Pending)
                {
                    if (Pending.Count >= expectedCount)
                    {
                        return true;
                    }
                }
                if (Environment.TickCount64 - start > timeoutMilliseconds)
                {
                    return false;
                }
                await Task.Delay(25).ConfigureAwait(false);
            }
        }

        public async Task<bool> WaitUntilCompletedAsync(int expectedCount, int timeoutMilliseconds = 5000)
        {
            var start = Environment.TickCount64;
            while (true)
            {
                lock (Completed)
                {
                    if (Completed.Count >= expectedCount)
                    {
                        return true;
                    }
                }
                if (Environment.TickCount64 - start > timeoutMilliseconds)
                {
                    return false;
                }
                await Task.Delay(25).ConfigureAwait(false);
            }
        }
    }

    private sealed class FakeNeuBellWebHookService : INeuBellWebHookService
    {
        private readonly List<NeuBellWebHook> _webHooks;

        public FakeNeuBellWebHookService(List<NeuBellWebHook> webHooks)
        {
            _webHooks = webHooks;
        }

        public Task<List<NeuBellWebHook>> GetEnabledListAsync()
        {
            return Task.FromResult(_webHooks.Where(webHook => webHook.IsEnabled).ToList());
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(_handler, disposeHandler: false);
        }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public sealed class CapturedRequest
        {
            public string Url { get; init; }
            public string Method { get; init; }
            public string Body { get; init; }
            public string ContentType { get; init; }
            public string Signature { get; init; }
        }

        private readonly ConcurrentQueue<CapturedRequest> _requests = new();

        public IReadOnlyCollection<CapturedRequest> Requests => _requests.ToArray();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _requests.Enqueue(new CapturedRequest
            {
                // AbsoluteUri 保留 URL 的原始百分号编码（ToString 会解码）
                Url = request.RequestUri?.AbsoluteUri,
                Method = request.Method.Method,
                Body = body,
                ContentType = request.Content?.Headers.ContentType?.MediaType,
                Signature = request.Headers.TryGetValues("X-NeuBell-Signature", out var values)
                    ? string.Join(",", values)
                    : null
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
        }

        public async Task WaitUntilAsync(int expectedCount, int timeoutMilliseconds = 5000)
        {
            var start = Environment.TickCount64;
            while (Requests.Count < expectedCount)
            {
                if (Environment.TickCount64 - start > timeoutMilliseconds)
                {
                    break;
                }
                await Task.Delay(25).ConfigureAwait(false);
            }
        }
    }
}
