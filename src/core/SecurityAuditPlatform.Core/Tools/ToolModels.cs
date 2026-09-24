namespace SecurityAuditPlatform.Core.Tools;

public sealed record ToolDefinition(string Id, string Name, string Runtime, string? Version, string? Executable, string? SourceRepository, string? LicenseSpdx);
