using System.Text.Json;
using SecurityAuditPlatform.Core.Findings;

namespace SecurityAuditPlatform.Infrastructure.Parsers;

public sealed class NucleiJsonlParser
{
    public IReadOnlyList<Finding> Parse(string jsonl)
    {
        var findings = new List<Finding>();
        foreach (var line in jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var info = root.TryGetProperty("info", out var i) ? i : default;
                var name = info.ValueKind == JsonValueKind.Object && info.TryGetProperty("name", out var n) ? n.GetString() ?? "Nuclei finding" : "Nuclei finding";
                var severityText = info.ValueKind == JsonValueKind.Object && info.TryGetProperty("severity", out var s) ? s.GetString() : null;
                var severity = severityText?.ToLowerInvariant() switch
                {
                    "critical" => FindingSeverity.Critical,
                    "high" => FindingSeverity.High,
                    "medium" => FindingSeverity.Medium,
                    "low" => FindingSeverity.Low,
                    _ => FindingSeverity.Informational
                };
                var host = root.TryGetProperty("host", out var h) ? h.GetString() : null;
                var matched = root.TryGetProperty("matched-at", out var m) ? m.GetString() : null;
                var description = matched is null ? "Nuclei reported a matching template." : $"Nuclei matched at {matched}.";
                findings.Add(new Finding(Guid.NewGuid(), name, description, severity, host, "Validate the finding manually, then remediate according to the affected technology and exposure.", [], DateTimeOffset.UtcNow));
            }
            catch (JsonException) { }
        }
        return findings;
    }
}
