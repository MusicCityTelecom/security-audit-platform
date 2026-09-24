using System.Text.Json;
using SecurityAuditPlatform.Core.Modules;

namespace SecurityAuditPlatform.Infrastructure.Modules;

public sealed class ModuleJsonStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public string Serialize(ModuleManifest manifest) => JsonSerializer.Serialize(manifest, Options);
    public ModuleManifest Deserialize(string json) => JsonSerializer.Deserialize<ModuleManifest>(json, Options) ?? throw new InvalidDataException("Invalid module manifest JSON.");
}
