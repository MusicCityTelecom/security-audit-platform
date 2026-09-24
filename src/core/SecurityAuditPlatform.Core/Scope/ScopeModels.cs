namespace SecurityAuditPlatform.Core.Scope;

public sealed record ScopeTarget(string Value, bool Excluded = false);

public sealed class AuthorizationScope
{
    private readonly List<ScopeTarget> _targets = new();
    public IReadOnlyList<ScopeTarget> Targets => _targets;
    public void Add(string value, bool excluded = false)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Scope value is required.", nameof(value));
        _targets.Add(new ScopeTarget(value.Trim(), excluded));
    }
    public bool IsAuthorized(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return false;
        var value = target.Trim();
        if (_targets.Any(x => x.Excluded && string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase))) return false;
        return _targets.Any(x => !x.Excluded && string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record Engagement(Guid Id, string Name, AuthorizationScope Scope, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt = null);
