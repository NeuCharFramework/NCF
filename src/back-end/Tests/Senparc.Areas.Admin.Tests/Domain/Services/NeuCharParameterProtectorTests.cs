/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharParameterProtectorTests.cs
    文件功能描述：Loop Task 与 Workflow 密码参数保护测试
----------------------------------------------------------------*/

using Microsoft.AspNetCore.DataProtection;
using Senparc.Areas.Admin.Domain.Services;
using System.Text.Json.Nodes;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuCharParameterProtectorTests
{
    [TestMethod]
    public void ProtectAndUnprotect_ShouldEncryptOnlyDeclaredSecretFields()
    {
        var protector = CreateProtector();

        var stored = protector.Protect(
            "{\"password\":\"top-secret\",\"query\":\"visible\"}",
            new[] { "PASSWORD" });

        Assert.IsFalse(stored.Contains("top-secret", StringComparison.Ordinal));
        StringAssert.Contains(stored, "visible");

        var plain = JsonNode.Parse(protector.Unprotect(stored))!.AsObject();
        Assert.AreEqual("top-secret", plain["password"]!.GetValue<string>());
        Assert.AreEqual("visible", plain["query"]!.GetValue<string>());
    }

    [TestMethod]
    public void MergeAndMask_ShouldRetainStoredSecretWithoutReturningCiphertextToClient()
    {
        var protector = CreateProtector();
        var stored = protector.Protect(
            "{\"password\":\"top-secret\",\"query\":\"old\"}",
            new[] { "password" });

        var merged = protector.MergeWithExisting(
            "{\"password\":\"\",\"query\":\"new\"}",
            stored,
            new[] { "password" });
        var mergedObject = JsonNode.Parse(merged)!.AsObject();
        Assert.AreEqual("top-secret", mergedObject["password"]!.GetValue<string>());
        Assert.AreEqual("new", mergedObject["query"]!.GetValue<string>());

        var masked = protector.MaskForClient(stored, new[] { "password" });
        var maskedObject = JsonNode.Parse(masked)!.AsObject();
        Assert.AreEqual(string.Empty, maskedObject["password"]!.GetValue<string>());
        Assert.IsFalse(masked.Contains("ncp:v1:", StringComparison.Ordinal));
    }

    [TestMethod]
    public void MergeAndMask_SecretOutputBinding_ShouldPreserveBindingInsteadOfOldCiphertext()
    {
        var protector = CreateProtector();
        var stored = protector.Protect(
            "{\"password\":\"top-secret\"}",
            new[] { "password" });
        const string binding =
            "{\"password\":{\"$source\":{\"nodeId\":\"upstream\",\"path\":\"$.Token\"}}}";

        var merged = protector.MergeWithExisting(binding, stored, new[] { "password" });
        var masked = protector.MaskForClient(merged, new[] { "password" });
        var value = JsonNode.Parse(masked)!.AsObject()["password"]!.AsObject();

        Assert.AreEqual("upstream", value["$source"]!["nodeId"]!.GetValue<string>());
        Assert.IsFalse(masked.Contains("top-secret", StringComparison.Ordinal));
        Assert.IsFalse(masked.Contains("ncp:v1:", StringComparison.Ordinal));
    }

    private static NeuCharParameterProtector CreateProtector() =>
        new(new EphemeralDataProtectionProvider());
}
