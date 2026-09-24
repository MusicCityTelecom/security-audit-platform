using System.Net;
using SecurityAuditPlatform.Infrastructure.Data;
using SecurityAuditPlatform.Infrastructure.Findings;

namespace SecurityAuditPlatform.Infrastructure.Reports;

public sealed class ReportService
{
    private readonly PlatformDatabase _database;
    private readonly FindingStore _findings;

    public ReportService(PlatformDatabase database, FindingStore findings) { _database = database; _findings = findings; }

    public string BuildHtml(string title)
    {
        var jobs = _database.ListJobs(500);
        var findings = _findings.List();
        static string E(string value) => WebUtility.HtmlEncode(value);
        var findingRows = string.Join("", findings.Select(f => $"<tr><td>{E(f.Severity.ToString())}</td><td>{E(f.Title)}</td><td>{E(f.Asset ?? "")}</td><td>{E(f.Description)}</td></tr>"));
        var jobRows = string.Join("", jobs.Select(j => $"<tr><td>{E(j.ModuleId)}</td><td>{E(j.Target)}</td><td>{E(j.State.ToString())}</td><td>{E(j.CreatedAt.ToString("u"))}</td></tr>"));
        return $"""<!doctype html><html><head><meta charset="utf-8"><title>{{E(title)}}</title><style>body{{font-family:Arial,sans-serif;margin:40px;color:#202733}}table{{border-collapse:collapse;width:100%;margin:20px 0}}th,td{{border:1px solid #ccd2dc;padding:8px;text-align:left}}th{{background:#eef1f6}}</style></head><body><h1>{E(title)}</h1><p>Generated {{DateTimeOffset.UtcNow:u}}</p><h2>Findings ({{findings.Count}})</h2><table><tr><th>Severity</th><th>Title</th><th>Asset</th><th>Description</th></tr>{{findingRows}}</table><h2>Jobs ({{jobs.Count}})</h2><table><tr><th>Module</th><th>Target</th><th>State</th><th>Created</th></tr>{{jobRows}}</table></body></html>""";
    }
}
