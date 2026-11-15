using System.Security.Claims;
using Senparc.Xncf.Tenant.OHS.Remote;

namespace Senparc.Areas.Admin.Tests;

[TestClass]
public class TenantHeaderAccessTests
{
    [DataTestMethod]
    [DataRow("TENANT-A", "TENANT-A", true)]
    [DataRow("tenant-a", "TENANT-A", true)]
    [DataRow("TENANT-A", "TENANT-B", false)]
    [DataRow(null, "TENANT-A", false)]
    [DataRow("TENANT-A", "", false)]
    public void HasTenantAccess_RequiresMatchingAuthenticatedTenantClaim(
        string? claimValue, string requestedKey, bool expected)
    {
        var claims = claimValue == null ? [] : new[] { new Claim("TenantKey", claimValue) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "AdminCookie"));

        Assert.AreEqual(expected, TenantHeaderAccessPolicy.HasTenantAccess(principal, requestedKey));
    }

    [TestMethod]
    public void HasTenantAccess_RejectsUnauthenticatedAndAmbiguousClaims()
    {
        var claim = new Claim("TenantKey", "TENANT-A");
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity([claim]));
        var ambiguous = new ClaimsPrincipal(new ClaimsIdentity(
            [claim, new Claim("TenantKey", "TENANT-B")], "AdminCookie"));

        Assert.IsFalse(TenantHeaderAccessPolicy.HasTenantAccess(unauthenticated, "TENANT-A"));
        Assert.IsFalse(TenantHeaderAccessPolicy.HasTenantAccess(ambiguous, "TENANT-A"));
    }

    [TestMethod]
    public void IsHeaderAllowed_RejectsCrossTenantAdminAndBearerRequests()
    {
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("TenantKey", "TENANT-A")], "AdminCookie"));

        Assert.IsTrue(TenantHeaderAccessPolicy.IsHeaderAllowed(["TENANT-A"], false, admin, admin));
        Assert.IsFalse(TenantHeaderAccessPolicy.IsHeaderAllowed(["TENANT-B"], false, admin, admin));
        Assert.IsFalse(TenantHeaderAccessPolicy.IsHeaderAllowed(["TENANT-A"], true, admin, admin));
        Assert.IsFalse(TenantHeaderAccessPolicy.IsHeaderAllowed(["TENANT-A", "TENANT-B"], false, admin, admin));
        Assert.IsFalse(TenantHeaderAccessPolicy.IsHeaderAllowed([" "], false, admin, admin));
    }

    [TestMethod]
    public void IsHeaderAllowed_OnlyAllowsAnonymousRequestsWithoutOtherCredentials()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.IsTrue(TenantHeaderAccessPolicy.IsHeaderAllowed(["TENANT-A"], false, null!, anonymous));
        Assert.IsFalse(TenantHeaderAccessPolicy.IsHeaderAllowed(["TENANT-A"], true, null!, anonymous));

        var globalAdmin = new ClaimsPrincipal(new ClaimsIdentity([], "AdminCookie"));
        Assert.IsFalse(TenantHeaderAccessPolicy.IsHeaderAllowed(["TENANT-A"], false, globalAdmin, anonymous));
    }
}
