using Moq;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.Core.WorkContext.Provider;
using System;

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


    #region 数据库访问策略（DB 覆盖代码属性）

    [TestMethod]
    public async Task OpenPolicy_ShouldOverrideCodeRestriction()
    {
        var workContext = CreateWorkContext(adminUserId: 42, roles: new[] { "viewer" });
        var permissions = new Mock<ICheckPermission>();
        permissions.Setup(checker => checker.HasPermissionAsync(It.IsAny<string[]>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        var service = new NeuCharPivotGlobalAccessService(workContext.Object, permissions.Object);

        // 代码基线要求 sandbox-admin 角色，数据库策略“开放”覆盖
        var denial = await service.GetDenialReasonAsync(
            CreateDescriptor(new[] { "sandbox-admin" }, new[] { "sandbox.execute" }),
            CreatePolicy(NeuCharFunctionProvitAccess.AccessModeOpen));

        Assert.IsNull(denial);
        permissions.Verify(
            checker => checker.HasPermissionAsync(It.IsAny<string[]>(), It.IsAny<int>()),
            Times.Never);
    }

    [TestMethod]
    public async Task DenyPolicy_ShouldOverrideCodeAllow()
    {
        var workContext = CreateWorkContext(adminUserId: 42, roles: new[] { "sandbox-admin" });
        var permissions = new Mock<ICheckPermission>();
        var service = new NeuCharPivotGlobalAccessService(workContext.Object, permissions.Object);

        // 代码基线完全允许，数据库策略“禁用”覆盖
        var denial = await service.GetDenialReasonAsync(
            CreateDescriptor(),
            CreatePolicy(NeuCharFunctionProvitAccess.AccessModeDeny));

        Assert.AreEqual("该 Function 的全局访问已被管理员策略禁用。", denial);
    }

    [TestMethod]
    public async Task RestrictedPolicy_ShouldAllowMatchedUserRoleOrPermission()
    {
        var service = new NeuCharPivotGlobalAccessService(
            CreateWorkContext(adminUserId: 42, roles: new[] { "viewer" }).Object,
            new Mock<ICheckPermission>().Object);
        var userPolicy = CreatePolicy(
            NeuCharFunctionProvitAccess.AccessModeRestricted,
            userIds: new[] { 42 });
        var rolePolicy = CreatePolicy(
            NeuCharFunctionProvitAccess.AccessModeRestricted,
            roleCodes: new[] { "VIEWER" });

        Assert.IsNull(await service.GetDenialReasonAsync(CreateDescriptor(), userPolicy));
        Assert.IsNull(await service.GetDenialReasonAsync(CreateDescriptor(), rolePolicy));
    }

    [TestMethod]
    public async Task RestrictedPolicy_ShouldAllowMatchedPermission()
    {
        var permissions = new Mock<ICheckPermission>();
        permissions.Setup(checker => checker.HasPermissionAsync(
                It.Is<string[]>(codes => codes.SequenceEqual(new[] { "sandbox.execute" })),
                42))
            .ReturnsAsync(true);
        var service = new NeuCharPivotGlobalAccessService(
            CreateWorkContext(adminUserId: 42).Object,
            permissions.Object);

        var denial = await service.GetDenialReasonAsync(
            CreateDescriptor(),
            CreatePolicy(
                NeuCharFunctionProvitAccess.AccessModeRestricted,
                permissionCodes: new[] { "sandbox.execute" }));

        Assert.IsNull(denial);
    }

    [TestMethod]
    public async Task RestrictedPolicy_ShouldDenyWhenNoSubjectMatches()
    {
        var permissions = new Mock<ICheckPermission>();
        permissions.Setup(checker => checker.HasPermissionAsync(It.IsAny<string[]>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        var service = new NeuCharPivotGlobalAccessService(
            CreateWorkContext(adminUserId: 42, roles: new[] { "viewer" }).Object,
            permissions.Object);

        var denial = await service.GetDenialReasonAsync(
            CreateDescriptor(),
            CreatePolicy(
                NeuCharFunctionProvitAccess.AccessModeRestricted,
                roleCodes: new[] { "sandbox-admin" },
                userIds: new[] { 7 }));

        Assert.AreEqual("当前账号未包含在该 Function 全局访问策略绑定的用户、角色或权限中。", denial);
    }

    [TestMethod]
    public async Task InheritPolicy_ShouldFallBackToCodeBaseline()
    {
        var service = new NeuCharPivotGlobalAccessService(
            CreateWorkContext(adminUserId: 17, roles: new[] { "viewer" }).Object,
            new Mock<ICheckPermission>().Object);
        var inheritPolicy = CreatePolicy(NeuCharFunctionProvitAccess.AccessModeInherit);

        // 继承策略（及 null 策略）都回退到代码属性：viewer 不匹配 sandbox-admin
        var denial = await service.GetDenialReasonAsync(
            CreateDescriptor(new[] { "sandbox-admin" }),
            inheritPolicy);
        var denialWithoutPolicy = await service.GetDenialReasonAsync(
            CreateDescriptor(new[] { "sandbox-admin" }));

        Assert.AreEqual("当前账号没有访问该全局 Function 的角色或权限。", denial);
        Assert.AreEqual(denial, denialWithoutPolicy);
    }

    [TestMethod]
    public void ResolveExposureAllowed_ShouldApplyDatabasePolicyOverCodeAttribute()
    {
        // 代码不允许全局：Open / Restricted 策略可开启
        Assert.IsFalse(NeuCharPivotGlobalAccessService.ResolveExposureAllowed(false, null));
        Assert.IsTrue(NeuCharPivotGlobalAccessService.ResolveExposureAllowed(
            false, CreatePolicy(NeuCharFunctionProvitAccess.AccessModeOpen)));
        Assert.IsTrue(NeuCharPivotGlobalAccessService.ResolveExposureAllowed(
            false, CreatePolicy(NeuCharFunctionProvitAccess.AccessModeRestricted, roleCodes: new[] { "a" })));

        // 代码允许全局：Deny 策略可关闭，Inherit / null 保持代码值
        Assert.IsFalse(NeuCharPivotGlobalAccessService.ResolveExposureAllowed(
            true, CreatePolicy(NeuCharFunctionProvitAccess.AccessModeDeny)));
        Assert.IsTrue(NeuCharPivotGlobalAccessService.ResolveExposureAllowed(
            true, CreatePolicy(NeuCharFunctionProvitAccess.AccessModeInherit)));
        Assert.IsTrue(NeuCharPivotGlobalAccessService.ResolveExposureAllowed(true, null));
    }

    [TestMethod]
    public void RestrictedPolicy_WithoutAnyBinding_ShouldThrow()
    {
        var policy = new NeuCharFunctionProvitAccess("sandbox", "Create");
        Assert.ThrowsException<ArgumentException>(() =>
            policy.Update(
                NeuCharFunctionProvitAccess.AccessModeRestricted,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<int>()));
    }

    [TestMethod]
    public void PolicyBindings_ShouldNormalizeAndRoundTrip()
    {
        var policy = new NeuCharFunctionProvitAccess(" sandbox ", " Create ");
        policy.Update(
            NeuCharFunctionProvitAccess.AccessModeRestricted,
            new[] { " A ", "a", "B" },
            new[] { "x.y" },
            new[] { 3, 1, 3, -1 });

        Assert.AreEqual("sandbox", policy.ModuleUid);
        Assert.AreEqual("Create", policy.FunctionKey);
        CollectionAssert.AreEqual(new[] { "A", "B" }, policy.RoleCodeList.ToList());
        CollectionAssert.AreEqual(new[] { "x.y" }, policy.PermissionCodeList.ToList());
        CollectionAssert.AreEqual(new[] { 3, 1 }, policy.AdminUserIdList.ToList());
        Assert.IsTrue(policy.HasAnySubjectBinding);
        Assert.IsTrue(policy.MatchesAdminUser(1));
        Assert.IsFalse(policy.MatchesAdminUser(2));
    }

    private static NeuCharFunctionProvitAccess CreatePolicy(
        int accessMode,
        IReadOnlyList<string> roleCodes = null,
        IReadOnlyList<string> permissionCodes = null,
        IReadOnlyList<int> userIds = null)
    {
        var policy = new NeuCharFunctionProvitAccess("sandbox", "Create");
        policy.Update(
            accessMode,
            roleCodes ?? (accessMode == NeuCharFunctionProvitAccess.AccessModeRestricted
                ? new[] { "placeholder-role" }
                : Array.Empty<string>()),
            permissionCodes ?? Array.Empty<string>(),
            userIds ?? Array.Empty<int>());
        return policy;
    }

    private static Mock<IAdminWorkContextProvider> CreateWorkContext(
        int adminUserId,
        string[] roles = null)
    {
        var workContext = new Mock<IAdminWorkContextProvider>();
        workContext.Setup(provider => provider.GetAdminWorkContext())
            .Returns(new AdminWorkContext
            {
                AdminUserId = adminUserId,
                RoleCodes = roles ?? Array.Empty<string>()
            });
        return workContext;
    }

    #endregion

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
