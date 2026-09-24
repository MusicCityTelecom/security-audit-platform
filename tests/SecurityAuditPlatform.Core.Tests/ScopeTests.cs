using SecurityAuditPlatform.Core.Scope;
namespace SecurityAuditPlatform.Core.Tests;
public sealed class ScopeTests
{
    [Fact] public void Exact_target_is_authorized(){var scope=new AuthorizationScope();scope.Add("10.10.10.5");Assert.True(scope.IsAuthorized("10.10.10.5"));}
    [Fact] public void Explicit_exclusion_overrides_inclusion(){var scope=new AuthorizationScope();scope.Add("10.10.10.5");scope.Add("10.10.10.5",true);Assert.False(scope.IsAuthorized("10.10.10.5"));}
}
