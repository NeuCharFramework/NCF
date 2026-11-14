/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceOwnershipTests.cs
    文件功能描述：工作区接口按当前账号过滤所有者


    创建标识：Senparc - 20261110

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Services;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public sealed class WorkspaceOwnershipTests
{
    [TestMethod]
    public void SelectOwned_ReturnsOnlyTheCurrentAccount()
    {
        var workspaces = new[]
        {
            Descriptor("ws_own", 7, "我的"),
            Descriptor("ws_other", 9, "别人的"),
            null
        };

        var owned = WorkspaceOwnership.SelectOwned(workspaces, 7);

        Assert.AreEqual(1, owned.Count);
        Assert.AreEqual("ws_own", owned[0].Id);
        Assert.AreEqual(0, WorkspaceOwnership.SelectOwned(workspaces, 0).Count);
    }

    [TestMethod]
    public void IsOwnedBy_RejectsMissingAndForeignWorkspaces()
    {
        Assert.IsTrue(WorkspaceOwnership.IsOwnedBy(Descriptor("ws_own", 7, "我的"), 7));
        Assert.IsFalse(WorkspaceOwnership.IsOwnedBy(Descriptor("ws_other", 9, "别人的"), 7));
        Assert.IsFalse(WorkspaceOwnership.IsOwnedBy(null, 7));
        Assert.IsFalse(WorkspaceOwnership.IsOwnedBy(Descriptor("ws_own", 7, "我的"), 0));
    }

    private static WorkspaceDescriptor Descriptor(string id, int ownerId, string name) =>
        new(id, ownerId, name, "/tmp/" + id, DateTime.UnixEpoch, DateTime.UnixEpoch);
}
