namespace SecurityAuditPlatform.Core.Findings;

public enum FindingSeverity { Informational, Low, Medium, High, Critical }
public sealed record EvidenceItem(Guid Id, string Kind, string Name, string ContentType, string Location, DateTimeOffset CollectedAt, long? SizeBytes = null, string? Sha256 = null);
public sealed record Finding(Guid Id, string Title, string Description, FindingSeverity Severity, string? Asset, string? Remediation, IReadOnlyList<Guid> EvidenceIds, DateTimeOffset CreatedAt);
