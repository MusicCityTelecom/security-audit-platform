using SecurityAuditPlatform.Core.Modules;
namespace SecurityAuditPlatform.Core.Tests;
public sealed class ModuleManifestValidatorTests
{
    private static ModuleManifest Valid() => new(1,"example.network.nmap","Nmap","0.1.0",ModuleCategory.Network,ModuleRuntime.Wsl,"module.py",["port-scanning"],["network"],new(["nmap"],[],["python3"]),NetworkBehavior.Active,new("GPL-2.0-or-later"),new("builtin"));
    [Fact] public void Valid_manifest_has_no_errors() => Assert.DoesNotContain(new ModuleManifestValidator().Validate(Valid()), x => x.IsError);
    [Fact] public void Rooted_entrypoint_is_rejected() => Assert.Contains(new ModuleManifestValidator().Validate(Valid() with { Entrypoint="/tmp/module.py" }), x => x.Code=="entrypoint.rooted" && x.IsError);
    [Fact] public void GitHub_source_requires_repository() => Assert.Contains(new ModuleManifestValidator().Validate(Valid() with { Source=new ModuleSource("github") }), x => x.Code=="source.repository" && x.IsError);
}
