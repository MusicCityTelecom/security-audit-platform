namespace SecurityAuditPlatform.Core.Jobs;

public enum JobState { Queued, Running, Succeeded, Failed, Canceled, TimedOut }
public sealed record Job(Guid Id, string ModuleId, Guid? EngagementId, string Target, JobState State, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt = null, DateTimeOffset? FinishedAt = null, int? ExitCode = null, string? Error = null);
