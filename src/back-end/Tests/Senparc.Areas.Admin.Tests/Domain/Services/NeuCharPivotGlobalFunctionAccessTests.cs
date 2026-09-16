using Moq;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.Core.WorkContext.Provider;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class NeuCharPivotGlobalFunctionAccessTests
{
    [TestMethod]
    public async Task UnrestrictedMapping_ShouldRequireAuthenticatedAdmin()
    {
        var workContext = new Mock<IAdminWorkContextProvider>();
        workContext.Setup(provider => provider.GetAdminWorkContext())
            .Returns(new AdminWorkContext { AdminUserId = 0 });
        var permissions = new Mock<ICheckPermission>();
        var service = new NeuCharPivotGlobalAccessService(workContext.Object, permissions.Object);

        var denial = await service.GetDenialReasonAsync(CreateDescriptor());

        Assert.AreEqual("请先登录后台管理员账号。", denial);
    }

    [TestMethod]
    public async Task RoleOrPermissionMapping_ShouldAllowEitherConfiguredGrant()
    {
        var workContext = new Mock<IAdminWorkContextProvider>();
        workContext.Setup(provider => provider.GetAdminWorkContext())
            .Returns(new AdminWorkContext
            {
                AdminUserId = 17,
                RoleCodes = new[] { "operator" }
            });
        var permissions = new Mock<ICheckPermission>();
        permissions.Setup(checker => checker.HasPermissionAsync(
                It.Is<string[]>(codes => codes.SequenceEqual(new[] { "sandbox.execute" })),
                17))
            .ReturnsAsync(true);
        var service = new NeuCharPivotGlobalAccessService(workContext.Object, permissions.Object);

        var roleDenial = await service.GetDenialReasonAsync(CreateDescriptor(
            new[] { "sandbox-admin" },
            new[] { "sandbox.execute" }));
        var roleMatched = NeuCharPivotGlobalAccessService.RoleMatches(
            workContext.Object.GetAdminWorkContext(),
            new[] { "OPERATOR" });

        Assert.IsNull(roleDenial);
        Assert.IsTrue(roleMatched);
        permissions.Verify(checker => checker.HasPermissionAsync(
            It.Is<string[]>(codes => codes.SequenceEqual(new[] { "sandbox.execute" })),
            17), Times.Once);
    }

    [TestMethod]
    public async Task RestrictedMapping_ShouldDenyWhenNoRoleOrPermissionMatches()
    {
        var workContext = new Mock<IAdminWorkContextProvider>();
        workContext.Setup(provider => provider.GetAdminWorkContext())
            .Returns(new AdminWorkContext
            {
                AdminUserId = 17,
                RoleCodes = new[] { "viewer" }
            });
        var permissions = new Mock<ICheckPermission>();
        permissions.Setup(checker => checker.HasPermissionAsync(It.IsAny<string[]>(), 17))
            .ReturnsAsync(false);
        var service = new NeuCharPivotGlobalAccessService(workContext.Object, permissions.Object);

        var denial = await service.GetDenialReasonAsync(CreateDescriptor(
            new[] { "sandbox-admin" },
            new[] { "sandbox.execute" }));

        Assert.AreEqual("当前账号没有访问该全局 Function 的角色或权限。", denial);
    }

    [TestMethod]
    public void GlobalFunctionKey_ShouldMatchMethodNameAliasWithoutChangingCanonicalKey()
    {
        var descriptor = CreateDescriptor();

        Assert.IsTrue(NeuCharPivotGlobalFunctionService.MatchesFunctionKey(descriptor, "Create"));
        Assert.IsTrue(NeuCharPivotGlobalFunctionService.MatchesFunctionKey(descriptor, descriptor.FunctionKey));
        Assert.IsTrue(NeuCharPivotGlobalFunctionService.MatchesFunctionKey(descriptor, descriptor.Name));
        Assert.IsFalse(NeuCharPivotGlobalFunctionService.MatchesFunctionKey(descriptor, "Unknown"));
    }

    private static NeuCharFunctionDescriptor CreateDescriptor(
        IReadOnlyList<string> roles = null,
        IReadOnlyList<string> permissions = null) =>
        new(
            "sandbox",
            "Sandbox",
            "1.0.0",
            true,
            "Create",
            "创建沙箱",
            "创建独立沙箱",
            Array.Empty<Senparc.Ncf.XncfBase.FunctionParameterInfo>(),
            AllowGlobalPivot: true,
            GlobalPivotRoleCodes: roles,
            GlobalPivotPermissionCodes: permissions,
            MethodName: "Create");
}
