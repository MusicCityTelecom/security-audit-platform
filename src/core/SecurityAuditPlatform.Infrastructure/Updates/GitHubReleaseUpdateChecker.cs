using System.Text.Json;

namespace SecurityAuditPlatform.Infrastructure.Updates;

public sealed record UpdateInfo(bool Available, string CurrentVersion, string? LatestVersion, string? ReleaseUrl, string? PublishedAt);
public sealed class GitHubReleaseUpdateChecker
{
    private readonly HttpClient _http;
    public GitHubReleaseUpdateChecker(HttpClient http) => _http=http;

    public async Task<UpdateInfo> CheckAsync(string owner, string repo, string currentVersion, CancellationToken ct=default)
    {
        using var request=new HttpRequestMessage(HttpMethod.Get,$"https://api.github.com/repos/{owner}/{repo}/releases/latest");
        request.Headers.UserAgent.ParseAdd("SecurityAuditPlatform/0.1");
        using var response=await _http.SendAsync(request,ct);
        if(!response.IsSuccessStatusCode) return new(false,currentVersion,null,null,null);
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root=doc.RootElement;
        var tag=root.TryGetProperty("tag_name",out var t)?t.GetString():null;
        var url=root.TryGetProperty("html_url",out var u)?u.GetString():null;
        var published=root.TryGetProperty("published_at",out var p)?p.GetString():null;
        var current = ParseVersion(currentVersion);
        var latest = ParseVersion(tag);
        var available = latest is not null && current is not null ? latest > current : !string.Equals(tag, currentVersion, StringComparison.OrdinalIgnoreCase);
        return new(available,currentVersion,tag,url,published);
    }
}
