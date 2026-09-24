namespace SecurityAuditPlatform.Core.Modules;

public interface IModule
{
    ModuleManifest Manifest { get; }
    Task<ModuleExecutionPlan> CreatePlanAsync(IReadOnlyDictionary<string, object?> inputs, CancellationToken cancellationToken = default);
}

public sealed record ModuleExecutionPlan(
    string ModuleId,
    ModuleRuntime Runtime,
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory,
    NetworkBehavior NetworkBehavior,
    bool RequiresExplicitConfirmation,
    IReadOnlyDictionary<string, string>? Environment = null);
