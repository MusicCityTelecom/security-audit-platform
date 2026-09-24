using System.Text.RegularExpressions;

namespace SecurityAuditPlatform.Core.Modules;

public sealed record ValidationIssue(string Code, string Message, bool IsError);

public sealed class ModuleManifestValidator
{
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{2,127}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex VersionPattern = new("^[0-9]+\\.[0-9]+\\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<ValidationIssue> Validate(ModuleManifest manifest)
    {
        var issues = new List<ValidationIssue>();
        if (manifest.SchemaVersion != 1) issues.Add(new("schema.unsupported", "Only module schema version 1 is currently supported.", true));
        if (!IdPattern.IsMatch(manifest.Id)) issues.Add(new("id.invalid", "Module ID must be 3-128 characters and contain only lowercase letters, digits, '.', '_' or '-'.", true));
        if (string.IsNullOrWhiteSpace(manifest.Name)) issues.Add(new("name.required", "Module name is required.", true));
        if (!VersionPattern.IsMatch(manifest.Version)) issues.Add(new("version.invalid", "Module version must use semantic version syntax.", true));
        if (string.IsNullOrWhiteSpace(manifest.Entrypoint)) issues.Add(new("entrypoint.required", "Module entrypoint is required.", true));
        if (Path.IsPathRooted(manifest.Entrypoint)) issues.Add(new("entrypoint.rooted", "Module entrypoint must be relative to the module directory.", true));
        if (manifest.NetworkBehavior is NetworkBehavior.Disruptive or NetworkBehavior.Destructive)
            issues.Add(new("network.confirmation", "High-impact modules require explicit operator confirmation at execution time.", false));
        if (manifest.Source.Type.Equals("github", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(manifest.Source.Repository))
            issues.Add(new("source.repository", "GitHub modules must declare a repository.", true));
        return issues;
    }
}
