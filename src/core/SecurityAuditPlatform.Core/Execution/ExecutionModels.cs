namespace SecurityAuditPlatform.Core.Execution;

public sealed record ExecutionRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null,
    TimeSpan? Timeout = null,
    bool CaptureOutput = true);

public sealed record ExecutionResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    bool TimedOut,
    bool Canceled)
{
    public TimeSpan Duration => FinishedAt - StartedAt;
}

public interface IExecutionProvider
{
    string Id { get; }
    ModuleRuntime Runtime { get; }
    Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default);
}
