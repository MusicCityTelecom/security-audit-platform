using System.Net;

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

        if (_targets.Any(x => x.Excluded && Matches(x.Value, value))) return false;
        return _targets.Any(x => !x.Excluded && Matches(x.Value, value));
    }

    private static bool Matches(string scope, string target)
    {
        if (string.Equals(scope, target, StringComparison.OrdinalIgnoreCase)) return true;

        if (scope.StartsWith("*.", StringComparison.Ordinal) &&
            target.EndsWith(scope[1..], StringComparison.OrdinalIgnoreCase) &&
            target.Length > scope.Length - 1) return true;

        if (IPAddress.TryParse(target, out var targetIp) && TryParseCidr(scope, out var network, out var prefix))
            return IsInNetwork(targetIp, network, prefix);

        return false;
    }

    private static bool TryParseCidr(string value, out IPAddress network, out int prefix)
    {
        network = IPAddress.None; prefix = 0;
        var parts = value.Split('/', 2);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out network) || !int.TryParse(parts[1], out prefix)) return false;
        var max = network.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        return prefix >= 0 && prefix <= max;
    }

    private static bool IsInNetwork(IPAddress address, IPAddress network, int prefix)
    {
        var a = address.GetAddressBytes();
        var n = network.GetAddressBytes();
        if (a.Length != n.Length) return false;
        var fullBytes = prefix / 8;
        var remainingBits = prefix % 8;
        for (var i = 0; i < fullBytes; i++) if (a[i] != n[i]) return false;
        if (remainingBits == 0) return true;
        var mask = (byte)(0xFF << (8 - remainingBits));
        return (a[fullBytes] & mask) == (n[fullBytes] & mask);
    }
}

public sealed record Engagement(Guid Id, string Name, AuthorizationScope Scope, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt = null);
