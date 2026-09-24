namespace SecurityAuditPlatform.Core.Assets;

public enum AssetKind
{
    Unknown,
    Host,
    Network,
    Domain,
    WebApplication,
    Service,
    Device,
    WirelessNetwork
}

public enum AssetStatus
{
    Discovered,
    Confirmed,
    InScope,
    OutOfScope,
    Retired
}

public sealed record Asset(
    Guid Id,
    string Value,
    AssetKind Kind,
    AssetStatus Status = AssetStatus.Discovered,
    string? Hostname = null,
    string? OperatingSystem = null,
    string? Vendor = null,
    string? MacAddress = null,
    int? Port = null,
    string? Protocol = null,
    string? Service = null,
    string? Version = null,
    string? Source = null,
    DateTimeOffset? FirstSeen = null,
    DateTimeOffset? LastSeen = null,
    IReadOnlyDictionary<string,string>? Attributes = null);

public sealed record AssetObservation(
    string Value,
    AssetKind Kind,
    string? Hostname = null,
    string? OperatingSystem = null,
    string? Vendor = null,
    string? MacAddress = null,
    int? Port = null,
    string? Protocol = null,
    string? Service = null,
    string? Version = null,
    string? Source = null,
    IReadOnlyDictionary<string,string>? Attributes = null);
