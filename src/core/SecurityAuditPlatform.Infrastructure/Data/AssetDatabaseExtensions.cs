using System.Text.Json;
using SecurityAuditPlatform.Core.Assets;

namespace SecurityAuditPlatform.Infrastructure.Data;

public static class AssetDatabaseExtensions
{
    public static void InitializeAssetSchema(this PlatformDatabase database)
    {
        using var c = database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS assets (
    id TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    kind INTEGER NOT NULL,
    status INTEGER NOT NULL,
    hostname TEXT NULL,
    operating_system TEXT NULL,
    vendor TEXT NULL,
    mac_address TEXT NULL,
    port INTEGER NULL,
    protocol TEXT NULL,
    service TEXT NULL,
    version TEXT NULL,
    source TEXT NULL,
    first_seen TEXT NULL,
    last_seen TEXT NULL,
    attributes TEXT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_assets_identity ON assets(value, kind, COALESCE(port, -1), COALESCE(protocol, ''));
CREATE INDEX IF NOT EXISTS ix_assets_last_seen ON assets(last_seen);";
        cmd.ExecuteNonQuery();
    }

    public static void SaveAsset(this PlatformDatabase database, Asset asset)
    {
        using var c = database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO assets(id,value,kind,status,hostname,operating_system,vendor,mac_address,port,protocol,service,version,source,first_seen,last_seen,attributes)
VALUES($id,$value,$kind,$status,$hostname,$os,$vendor,$mac,$port,$protocol,$service,$version,$source,$first,$last,$attributes)
ON CONFLICT(id) DO UPDATE SET
 value=$value,kind=$kind,status=$status,hostname=$hostname,operating_system=$os,vendor=$vendor,
 mac_address=$mac,port=$port,protocol=$protocol,service=$service,version=$version,source=$source,
 first_seen=$first,last_seen=$last,attributes=$attributes;";
        Add(cmd,"$id",asset.Id.ToString());
        Add(cmd,"$value",asset.Value);
        Add(cmd,"$kind",(int)asset.Kind);
        Add(cmd,"$status",(int)asset.Status);
        Add(cmd,"$hostname",asset.Hostname);
        Add(cmd,"$os",asset.OperatingSystem);
        Add(cmd,"$vendor",asset.Vendor);
        Add(cmd,"$mac",asset.MacAddress);
        Add(cmd,"$port",asset.Port);
        Add(cmd,"$protocol",asset.Protocol);
        Add(cmd,"$service",asset.Service);
        Add(cmd,"$version",asset.Version);
        Add(cmd,"$source",asset.Source);
        Add(cmd,"$first",asset.FirstSeen?.ToString("O"));
        Add(cmd,"$last",asset.LastSeen?.ToString("O"));
        Add(cmd,"$attributes",asset.Attributes is null ? null : JsonSerializer.Serialize(asset.Attributes));
        cmd.ExecuteNonQuery();
    }

    public static Asset? FindAsset(this PlatformDatabase database, Guid id)
    {
        using var c = database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM assets WHERE id=$id LIMIT 1;";
        Add(cmd,"$id",id.ToString());
        using var r = cmd.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }

    public static Asset? FindAsset(this PlatformDatabase database, string value, AssetKind kind, int? port, string? protocol)
    {
        using var c = database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM assets WHERE value=$value AND kind=$kind AND COALESCE(port,-1)=COALESCE($port,-1) AND COALESCE(protocol,'')=COALESCE($protocol,'') LIMIT 1;";
        Add(cmd,"$value",value.Trim());
        Add(cmd,"$kind",(int)kind);
        Add(cmd,"$port",port);
        Add(cmd,"$protocol",protocol);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }

    public static IReadOnlyList<Asset> ListAssets(this PlatformDatabase database, int limit = 1000)
    {
        using var c = database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM assets ORDER BY COALESCE(last_seen,'') DESC LIMIT $limit;";
        Add(cmd,"$limit",Math.Clamp(limit,1,5000));
        using var r = cmd.ExecuteReader();
        var list = new List<Asset>();
        while (r.Read()) list.Add(Read(r));
        return list;
    }

    private static Asset Read(Microsoft.Data.Sqlite.SqliteDataReader r)
    {
        IReadOnlyDictionary<string,string>? attributes = null;
        if (!r.IsDBNull(15))
        {
            try { attributes = JsonSerializer.Deserialize<Dictionary<string,string>>(r.GetString(15)); }
            catch (JsonException) { }
        }

        return new Asset(
            Guid.Parse(r.GetString(0)), r.GetString(1), (AssetKind)r.GetInt32(2), (AssetStatus)r.GetInt32(3),
            Nullable(r,4), Nullable(r,5), Nullable(r,6), Nullable(r,7),
            r.IsDBNull(8) ? null : r.GetInt32(8), Nullable(r,9), Nullable(r,10), Nullable(r,11),
            Nullable(r,12), ParseDate(r,13), ParseDate(r,14), attributes);
    }

    private static string? Nullable(Microsoft.Data.Sqlite.SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static DateTimeOffset? ParseDate(Microsoft.Data.Sqlite.SqliteDataReader r, int i) => r.IsDBNull(i) ? null : DateTimeOffset.Parse(r.GetString(i));
    private static void Add(Microsoft.Data.Sqlite.SqliteCommand cmd, string name, object? value) => cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
}
