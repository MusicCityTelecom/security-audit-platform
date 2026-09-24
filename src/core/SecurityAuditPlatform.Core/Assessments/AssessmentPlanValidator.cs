namespace SecurityAuditPlatform.Core.Assessments;

public sealed class AssessmentPlanValidator
{
    public IReadOnlyList<string> Validate(AssessmentPlan plan)
    {
        var errors = new List<string>();
        if (plan.Id == Guid.Empty) errors.Add("Assessment ID is required.");
        if (string.IsNullOrWhiteSpace(plan.Name)) errors.Add("Assessment name is required.");
        if (plan.EngagementId == Guid.Empty) errors.Add("An engagement is required.");
        if (plan.Steps.Count == 0) errors.Add("At least one assessment step is required.");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in plan.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id)) errors.Add("Every assessment step requires an ID.");
            else if (!ids.Add(step.Id)) errors.Add($"Duplicate assessment step ID '{step.Id}'.");
            if (string.IsNullOrWhiteSpace(step.Name)) errors.Add($"Step '{step.Id}' requires a name.");
            if (step.RequiresConfirmation && !AssessmentImpactPolicy.RequiresExplicitConfirmation(step.Impact))
                errors.Add($"Step '{step.Id}' requests confirmation but is not classified as disruptive or destructive.");
        }

        return errors;
    }
}
