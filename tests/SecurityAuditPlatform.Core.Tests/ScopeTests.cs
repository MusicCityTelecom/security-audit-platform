using SecurityAuditPlatform.Core.Scope;

namespace SecurityAuditPlatform.Core.Tests;

public sealed class ScopeTests
{
    [Fact]
    public void Authorizes_ipv4_cidr_and_honors_exclusion()
    {
        var scope = new AuthorizationScope();
        scope.Add("10.20.30.0/24");
        scope.Add("10.20.30.50", excluded: true);
        Assert.True(scope.IsAuthorized("10.20.30.10"));
        Assert.False(scope.IsAuthorized("10.20.31.10"));
        Assert.False(scope.IsAuthorized("10.20.30.50"));
    }

    [Fact]
    public void Supports_ipv6_cidr()
    {
        var scope = new AuthorizationScope();
        scope.Add("2001:db8::/32");
        Assert.True(scope.IsAuthorized("2001:db8:1234::1"));
        Assert.False(scope.IsAuthorized("2001:db9::1"));
    }

    [Fact]
    public void Supports_wildcard_hostnames()
    {
        var scope = new AuthorizationScope();
        scope.Add("*.example.test");
        Assert.True(scope.IsAuthorized("app.example.test"));
        Assert.False(scope.IsAuthorized("example.test"));
        Assert.False(scope.IsAuthorized("app.other.test"));
    }
}
