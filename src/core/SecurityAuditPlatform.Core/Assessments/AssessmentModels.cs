namespace SecurityAuditPlatform.Core.Assessments;

public enum AssessmentPhase
{
    Discovery,
    Enumeration,
    VulnerabilityAssessment,
    Validation,
    EvidenceCollection,
    Findings,
    Reporting
}

public enum AssessmentActionImpact
{
    Passive,
    Active,
    Disruptive,
    Destructive
}

public enum AssessmentStatus
{
    Draft,
    Ready,
    Running,
    Paused,
    Completed,
    Failed,
    Canceled
}

public sealed record AssessmentStep(
    string Id,
    AssessmentPhase Phase,
    string Name,
    string? ModuleId = null,
    AssessmentActionImpact Impact = AssessmentActionImpact.Active,
    bool RequiresConfirmation = false,
    bool Enabled = true,
    IReadOnlyDictionary<string,string>? Parameters = null);

public sealed record AssessmentPlan(
    Guid Id,
    string Name,
    Guid EngagementId,
    IReadOnlyList<AssessmentStep> Steps,
    AssessmentStatus Status = AssessmentStatus.Draft,
    DateTimeOffset? CreatedAt = null);

public sealed record AssessmentStepResult(
    string StepId,
    AssessmentStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt = null,
    Guid? JobId = null,
    string? Error = null);

public static class AssessmentImpactPolicy
{
    public static bool RequiresExplicitConfirmation(AssessmentActionImpact impact) =>
        impact is AssessmentActionImpact.Disruptive or AssessmentActionImpact.Destructive;

    public static bool IsAllowed(AssessmentActionImpact impact, bool confirmed) =>
        !RequiresExplicitConfirmation(impact) || confirmed;
}
