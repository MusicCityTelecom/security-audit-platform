using System.IO.Compression;
using System.Text.RegularExpressions;
using SecurityAuditPlatform.Core.Modules;

namespace SecurityAuditPlatform.Infrastructure.Modules;

public sealed record GitHubModuleImportResult(string ModuleId, string Version, string Repository, string Revision, string InstalledDirectory, IReadOnlyList<ValidationIssue> Issues);

public sealed class GitHubModuleImporter
{
    private readonly HttpClient _http;
    private readonly ModuleManifestYamlStore _yaml;
    private readonly ModuleManifestValidator _validator;
    private readonly string _modulesRoot;

    public GitHubModuleImporter(HttpClient http, ModuleManifestYamlStore yaml, ModuleManifestValidator validator, string modulesRoot)
    {
        _http = http; _yaml = yaml; _validator = validator; _modulesRoot = modulesRoot;
    }

    public async Task<GitHubModuleImportResult> ImportAsync(string repositoryUrl, string? revision = null, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only github.com repositories are accepted.", nameof(repositoryUrl));
        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) throw new ArgumentException("Repository URL must contain owner and repository.", nameof(repositoryUrl));

        var owner = parts[0];
        var repo = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
        var branch = string.IsNullOrWhiteSpace(revision) ? await GetDefaultBranch(owner, repo, cancellationToken) : revision!;
        var zipUrl = $"https://codeload.github.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/zip/refs/heads/{Uri.EscapeDataString(branch)}";

        using var response = await _http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var staging = Path.Combine(Path.GetTempPath(), "sap-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            foreach (var entry in archive.Entries)
            {
                var normalized = entry.FullName.Replace('\\', '/');
                if (normalized.StartsWith('/') || normalized.Split('/').Any(x => x == ".."))
                    throw new InvalidDataException($"Unsafe archive path: {entry.FullName}");
                var destination = Path.GetFullPath(Path.Combine(staging, normalized));
                if (!destination.StartsWith(Path.GetFullPath(staging) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Archive path escapes staging directory: {entry.FullName}");
                if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(destination); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await using var input = entry.Open();
                await using var output = File.Create(destination);
                await input.CopyToAsync(output, cancellationToken);
            }

            var manifestPath = Directory.EnumerateFiles(staging, "module.yaml", SearchOption.AllDirectories).FirstOrDefault();
            if (manifestPath is null) throw new InvalidDataException("No module.yaml was found in the repository.");
            var manifest = _yaml.Deserialize(await File.ReadAllTextAsync(manifestPath, cancellationToken));
            var issues = _validator.Validate(manifest);
            if (issues.Any(x => x.IsError)) return new GitHubModuleImportResult(manifest.Id, manifest.Version, $"{owner}/{repo}", branch, "", issues);

            var sourceDirectory = Path.GetDirectoryName(manifestPath)!;
            var installDirectory = Path.Combine(_modulesRoot, manifest.Id);
            if (Directory.Exists(installDirectory)) throw new IOException($"Module '{manifest.Id}' is already installed.");
            Directory.CreateDirectory(_modulesRoot);
            CopyTree(sourceDirectory, installDirectory);

            return new GitHubModuleImportResult(manifest.Id, manifest.Version, $"{owner}/{repo}", branch, installDirectory, issues);
        }
        finally
        {
            try { Directory.Delete(staging, true); } catch { }
        }
    }

    private async Task<string> GetDefaultBranch(string owner, string repo, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}");
        request.Headers.UserAgent.ParseAdd("SecurityAuditPlatform/0.1");
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
        return json.GetProperty("default_branch").GetString() ?? "main";
    }

    private static void CopyTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.Split(Path.DirectorySeparatorChar).Any(x => x == "..")) throw new InvalidDataException("Invalid relative module path.");
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }
}
