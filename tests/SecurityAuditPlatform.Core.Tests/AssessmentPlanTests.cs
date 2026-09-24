using SecurityAuditPlatform.Core.Assessments;

namespace SecurityAuditPlatform.Core.Tests;

public sealed class AssessmentPlanTests
{
    [Fact]
    public void Validator_RejectsDuplicateStepIds()
    {
        var id = Guid.NewGuid();
        var plan = new AssessmentPlan(id, "Internal", Guid.NewGuid(), [
            new AssessmentStep("discover", AssessmentPhase.Discovery, "Discovery"),
            new AssessmentStep("discover", AssessmentPhase.Enumeration, "Enumeration")
        ]);

        var errors = new AssessmentPlanValidator().Validate(plan);

        Assert.Contains(errors, x => x.Contains("Duplicate assessment step ID", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(AssessmentActionImpact.Passive, false, true)]
    [InlineData(AssessmentActionImpact.Active, false, true)]
    [InlineData(AssessmentActionImpact.Disruptive, false, false)]
    [InlineData(AssessmentActionImpact.Disruptive, true, true)]
    [InlineData(AssessmentActionImpact.Destructive, false, false)]
    [InlineData(AssessmentActionImpact.Destructive, true, true)]
    public void ImpactPolicy_ControlsHighImpactOperations(AssessmentActionImpact impact, bool confirmed, bool expected) =>
        Assert.Equal(expected, AssessmentImpactPolicy.IsAllowed(impact, confirmed));
}
