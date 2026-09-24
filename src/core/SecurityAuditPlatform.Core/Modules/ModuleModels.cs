namespace SecurityAuditPlatform.Core.Modules;

public enum ModuleRuntime
{
    Windows,
    Wsl,
    Container,
    Remote
}

public enum NetworkBehavior
{
    Passive,
    Active,
    Disruptive,
    Destructive
}

public enum ModuleCategory
{
    Recon,
    Network,
    Web,
    Wireless,
    Identity,
    ActiveDirectory,
    Vulnerability,
    Exploitation,
    PostExploitation,
    CredentialAudit,
    Forensics,
    MalwareAnalysis,
    Firmware,
    Reporting,
    Utility
}

public sealed record ModuleDependency(
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> Hardware,
    IReadOnlyList<string> Runtimes);

public sealed record ModuleLicense(string Spdx);

public sealed record ModuleSource(string Type, string? Repository = null, string? Ref = null);

public sealed record ModuleManifest(
    int SchemaVersion,
    string Id,
    string Name,
    string Version,
    ModuleCategory Category,
    ModuleRuntime Runtime,
    string Entrypoint,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Privileges,
    ModuleDependency Requires,
    NetworkBehavior NetworkBehavior,
    ModuleLicense License,
    ModuleSource Source,
    IReadOnlyDictionary<string, object?>? Inputs = null,
    IReadOnlyDictionary<string, object?>? Outputs = null,
    IReadOnlyList<string>? Evidence = null);
