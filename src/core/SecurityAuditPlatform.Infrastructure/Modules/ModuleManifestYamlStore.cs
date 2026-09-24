using SecurityAuditPlatform.Core.Modules;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SecurityAuditPlatform.Infrastructure.Modules;

public sealed class ModuleManifestYamlStore
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public ModuleManifest Deserialize(string yaml)
    {
        var document = _deserializer.Deserialize<YamlModuleDocument>(yaml)
            ?? throw new InvalidDataException("Module manifest is empty.");

        var requires = document.Requires ?? new YamlRequires();
        var network = document.Network ?? new YamlNetwork();
        var license = document.License ?? new YamlLicense();
        var source = document.Source ?? new YamlSource();

        if (!Enum.TryParse<ModuleCategory>(document.Category ?? "Utility", true, out var category))
            throw new InvalidDataException($"Unknown module category '{document.Category}'.");
        if (!Enum.TryParse<ModuleRuntime>(document.Runtime ?? "Windows", true, out var runtime))
            throw new InvalidDataException($"Unknown module runtime '{document.Runtime}'.");
        if (!Enum.TryParse<NetworkBehavior>(network.Behavior ?? "Passive", true, out var behavior))
            throw new InvalidDataException($"Unknown network behavior '{network.Behavior}'.");

        return new ModuleManifest(
            document.SchemaVersion,
            document.Id ?? throw new InvalidDataException("Module id is required."),
            document.Name ?? throw new InvalidDataException("Module name is required."),
            document.Version ?? throw new InvalidDataException("Module version is required."),
            category, runtime,
            document.Entrypoint ?? throw new InvalidDataException("Module entrypoint is required."),
            document.Capabilities ?? [],
            document.Privileges ?? [],
            new ModuleDependency(requires.Tools ?? [], requires.Hardware ?? [], requires.Runtimes ?? []),
            behavior,
            new ModuleLicense(license.Spdx ?? "UNKNOWN"),
            new ModuleSource(source.Type ?? "local", source.Repository, source.Ref),
            document.Inputs, document.Outputs, document.Evidence);
    }

    private sealed class YamlModuleDocument
    {
        public int SchemaVersion { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Category { get; set; }
        public string? Runtime { get; set; }
        public string? Entrypoint { get; set; }
        public List<string>? Capabilities { get; set; }
        public List<string>? Privileges { get; set; }
        public YamlRequires? Requires { get; set; }
        public YamlNetwork? Network { get; set; }
        public YamlLicense? License { get; set; }
        public YamlSource? Source { get; set; }
        public Dictionary<string, object?>? Inputs { get; set; }
        public Dictionary<string, object?>? Outputs { get; set; }
        public List<string>? Evidence { get; set; }
    }

    private sealed class YamlRequires { public List<string>? Tools { get; set; } public List<string>? Hardware { get; set; } public List<string>? Runtimes { get; set; } }
    private sealed class YamlNetwork { public string? Behavior { get; set; } }
    private sealed class YamlLicense { public string? Spdx { get; set; } }
    private sealed class YamlSource { public string? Type { get; set; } public string? Repository { get; set; } public string? Ref { get; set; } }
}
