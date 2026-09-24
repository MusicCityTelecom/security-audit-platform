using System.Collections.Concurrent;
using SecurityAuditPlatform.Core.Scope;

namespace SecurityAuditPlatform.Infrastructure.Engagements;

public sealed class EngagementService
{
    private readonly ConcurrentDictionary<Guid, Engagement> _engagements = new();

    public Engagement Create(string name, IEnumerable<(string Value, bool Excluded)> targets, DateTimeOffset? expiresAt = null)
    {
        var scope = new AuthorizationScope();
        foreach (var target in targets) scope.Add(target.Value, target.Excluded);
        var engagement = new Engagement(Guid.NewGuid(), name, scope, DateTimeOffset.UtcNow, expiresAt);
        _engagements[engagement.Id] = engagement;
        return engagement;
    }

    public Engagement? Get(Guid id) => _engagements.TryGetValue(id, out var engagement) ? engagement : null;

    public bool IsAuthorized(Guid id, string target)
    {
        var engagement = Get(id);
        if (engagement is null) return false;
        if (engagement.ExpiresAt is not null && engagement.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        return engagement.Scope.IsAuthorized(target);
    }

    public IReadOnlyList<Engagement> List() => _engagements.Values.OrderByDescending(x => x.CreatedAt).ToArray();
}
