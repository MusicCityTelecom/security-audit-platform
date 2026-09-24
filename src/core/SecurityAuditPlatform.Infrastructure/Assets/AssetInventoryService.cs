using SecurityAuditPlatform.Core.Assets;
using SecurityAuditPlatform.Infrastructure.Data;

namespace SecurityAuditPlatform.Infrastructure.Assets;

public sealed class AssetInventoryService
{
    private readonly PlatformDatabase _database;

    public AssetInventoryService(PlatformDatabase database) => _database = database;

    public IReadOnlyList<Asset> List(int limit = 1000) => _database.ListAssets(limit);

    public Asset Upsert(AssetObservation observation)
    {
        if (string.IsNullOrWhiteSpace(observation.Value))
            throw new ArgumentException("Asset value is required.", nameof(observation));

        var now = DateTimeOffset.UtcNow;
        var existing = _database.FindAsset(observation.Value, observation.Kind, observation.Port, observation.Protocol);
        var asset = existing is null
            ? new Asset(Guid.NewGuid(), observation.Value.Trim(), observation.Kind, AssetStatus.Discovered,
                observation.Hostname, observation.OperatingSystem, observation.Vendor, observation.MacAddress,
                observation.Port, observation.Protocol, observation.Service, observation.Version, observation.Source,
                now, now, observation.Attributes)
            : existing with
            {
                Hostname = observation.Hostname ?? existing.Hostname,
                OperatingSystem = observation.OperatingSystem ?? existing.OperatingSystem,
                Vendor = observation.Vendor ?? existing.Vendor,
                MacAddress = observation.MacAddress ?? existing.MacAddress,
                Service = observation.Service ?? existing.Service,
                Version = observation.Version ?? existing.Version,
                Source = observation.Source ?? existing.Source,
                LastSeen = now,
                Attributes = Merge(existing.Attributes, observation.Attributes)
            };

        _database.SaveAsset(asset);
        return asset;
    }

    public int Import(IEnumerable<AssetObservation> observations)
    {
        var count = 0;
        foreach (var observation in observations)
        {
            Upsert(observation);
            count++;
        }
        return count;
    }

    public bool SetStatus(Guid id, AssetStatus status)
    {
        var asset = _database.FindAsset(id);
        if (asset is null) return false;
        _database.SaveAsset(asset with { Status = status, LastSeen = DateTimeOffset.UtcNow });
        return true;
    }

    private static IReadOnlyDictionary<string,string>? Merge(
        IReadOnlyDictionary<string,string>? current,
        IReadOnlyDictionary<string,string>? incoming)
    {
        if (current is null && incoming is null) return null;
        var merged = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if (current is not null) foreach (var pair in current) merged[pair.Key] = pair.Value;
        if (incoming is not null) foreach (var pair in incoming) merged[pair.Key] = pair.Value;
        return merged;
    }
}
