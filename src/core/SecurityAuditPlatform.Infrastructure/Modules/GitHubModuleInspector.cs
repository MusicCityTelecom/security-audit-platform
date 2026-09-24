using System.Net.Http.Json;
using System.Text.Json;

namespace SecurityAuditPlatform.Infrastructure.Modules;

public sealed record GitHubModuleInspection(
    string Repository,
    string DefaultBranch,
    string? Description,
    string? License,
    bool HasModuleManifest,
    bool HasInstallScript,
    IReadOnlyList<string> CandidateFiles,
    IReadOnlyList<string> Warnings);

public sealed class GitHubModuleInspector(HttpClient http)
{
    public async Task<GitHubModuleInspection> InspectAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only github.com repository URLs are accepted.", nameof(repositoryUrl));

        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) throw new ArgumentException("Repository URL must contain owner and repository.", nameof(repositoryUrl));

        var owner = parts[0];
        var repo = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}");
        request.Headers.UserAgent.ParseAdd("SecurityAuditPlatform/0.1");
        var metadata = await (await http.SendAsync(request, cancellationToken)).Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var branch = metadata.GetProperty("default_branch").GetString() ?? "main";
        var description = metadata.TryGetProperty("description", out var d) ? d.GetString() : null;
        var license = metadata.TryGetProperty("license", out var l) && l.ValueKind == JsonValueKind.Object && l.TryGetProperty("spdx_id", out var spdx) ? spdx.GetString() : null;

        using var treeRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/git/trees/{Uri.EscapeDataString(branch)}?recursive=1");
        treeRequest.Headers.UserAgent.ParseAdd("SecurityAuditPlatform/0.1");
        var tree = await (await http.SendAsync(treeRequest, cancellationToken)).Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var candidates = tree.GetProperty("tree").EnumerateArray().Select(x => x.GetProperty("path").GetString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        var warnings = new List<string>();
        if (!candidates.Any(x => x.Equals("module.yaml", StringComparison.OrdinalIgnoreCase) || x.EndsWith("/module.yaml", StringComparison.OrdinalIgnoreCase)))
            warnings.Add("No module.yaml was found; the importer will require a manifest mapping step.");
        if (candidates.Any(IsInstallScript)) warnings.Add("Repository contains install/bootstrap scripts. They are inspection-only and must not be executed automatically.");
        if (candidates.Any(x => x.Contains(".github/workflows/", StringComparison.OrdinalIgnoreCase))) warnings.Add("Repository contains GitHub Actions workflows; workflows are not executed by the importer.");

        return new GitHubModuleInspection($"{owner}/{repo}", branch, description, license,
            candidates.Any(x => x.Equals("module.yaml", StringComparison.OrdinalIgnoreCase) || x.EndsWith("/module.yaml", StringComparison.OrdinalIgnoreCase)),
            candidates.Any(IsInstallScript), candidates.Take(200).ToArray(), warnings);
    }

    private static bool IsInstallScript(string path) =>
        path.EndsWith("install.sh", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("install.ps1", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("setup.sh", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("setup.ps1", StringComparison.OrdinalIgnoreCase);
}
