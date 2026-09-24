using SecurityAuditPlatform.Core.Modules;
using SecurityAuditPlatform.Infrastructure.Data;
using SecurityAuditPlatform.Infrastructure.Engagements;
using SecurityAuditPlatform.Infrastructure.Execution;
using SecurityAuditPlatform.Infrastructure.Findings;
using SecurityAuditPlatform.Infrastructure.Reports;
using SecurityAuditPlatform.Infrastructure.Parsers;
using SecurityAuditPlatform.Infrastructure.Jobs;
using SecurityAuditPlatform.Infrastructure.Modules;

var builder = WebApplication.CreateBuilder(args);
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
var modulesDirectory = Path.Combine(builder.Environment.ContentRootPath, "modules");

builder.Services.AddSingleton<ModuleManifestValidator>();
builder.Services.AddSingleton<ModuleManifestYamlStore>();
builder.Services.AddSingleton<ModuleJsonStore>();
builder.Services.AddSingleton<ProcessExecutionProvider>();
builder.Services.AddSingleton<IExecutionProvider>(sp => sp.GetRequiredService<ProcessExecutionProvider>());
builder.Services.AddSingleton<WslExecutionProvider>();
builder.Services.AddSingleton<IExecutionProvider>(sp => sp.GetRequiredService<WslExecutionProvider>());
builder.Services.AddSingleton<PlatformDatabase>(_ => new PlatformDatabase(Path.Combine(dataDirectory, "platform.db")));
builder.Services.AddSingleton<EngagementService>();
builder.Services.AddSingleton<ExecutionEvidenceStore>();
builder.Services.AddSingleton<SecurityAuditPlatform.Infrastructure.Settings.SettingsService>();
builder.Services.AddSingleton<FindingStore>();
builder.Services.AddSingleton<ReportService>();
builder.Services.AddSingleton<NmapXmlParser>();
builder.Services.AddSingleton<NucleiJsonlParser>();
builder.Services.AddSingleton<IModuleRegistry>(sp => new FileModuleRegistry(modulesDirectory, sp.GetRequiredService<ModuleManifestYamlStore>(), sp.GetRequiredService<ModuleManifestValidator>()));
builder.Services.AddHttpClient<GitHubModuleImporter>();
builder.Services.AddHttpClient<GitHubModuleInspector>();
builder.Services.AddSingleton<JobScheduler>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<JobScheduler>());

var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "security-audit-platform", version = "0.1.0", utc = DateTimeOffset.UtcNow }));

app.MapGet("/api/modules", (IModuleRegistry registry) => Results.Ok(registry.List().Select(x => new {
    x.Manifest.Id, x.Manifest.Name, x.Manifest.Version, x.Manifest.Category, x.Manifest.Runtime,
    x.Manifest.NetworkBehavior, x.Manifest.Capabilities, x.Manifest.Privileges,
    valid = x.ValidationIssues.All(i => !i.IsError), issues = x.ValidationIssues
})));

app.MapPost("/api/modules/refresh", (IModuleRegistry registry) => { registry.Refresh(); return Results.Ok(registry.List().Count); });
app.MapPost("/api/modules/validate", (ModuleManifest manifest, ModuleManifestValidator validator) => { var issues = validator.Validate(manifest); return Results.Ok(new { valid = issues.All(x => !x.IsError), issues }); });
app.MapPost("/api/modules/serialize", (ModuleManifest manifest, ModuleJsonStore store) => Results.Text(store.Serialize(manifest), "application/json"));
app.MapPost("/api/modules/inspect-github", async (GitHubModuleInspectRequest request, GitHubModuleInspector inspector, CancellationToken ct) => Results.Ok(await inspector.InspectAsync(request.RepositoryUrl, ct)));
app.MapPost("/api/modules/import-github", async (GitHubModuleImportRequest request, GitHubModuleImporter importer, IModuleRegistry registry, CancellationToken ct) =>
{
    var result = await importer.ImportAsync(request.RepositoryUrl, request.Revision, ct);
    if (result.Issues.Any(x => x.IsError)) return Results.BadRequest(result);
    registry.Refresh();
    return Results.Ok(result);
});

app.MapGet("/api/settings", (SecurityAuditPlatform.Infrastructure.Settings.SettingsService settings) => Results.Ok(settings.List()));
app.MapPut("/api/settings", (SecurityAuditPlatform.Infrastructure.Settings.SetSettingRequest request, SecurityAuditPlatform.Infrastructure.Settings.SettingsService settings) =>
{
    try { return Results.Ok(settings.Set(request)); } catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
});
app.MapDelete("/api/settings/{key}", (string key, SecurityAuditPlatform.Infrastructure.Settings.SettingsService settings) =>
    settings.Delete(key) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/settings/directories", (SecurityAuditPlatform.Infrastructure.Settings.SettingsService settings) =>
    Results.Ok(new {
        data = settings.GetValue("directories.data") ?? "",
        modules = settings.GetValue("directories.modules") ?? "",
        evidence = settings.GetValue("directories.evidence") ?? "",
        reports = settings.GetValue("directories.reports") ?? "",
        tools = settings.GetValue("directories.tools") ?? "",
        runtimes = settings.GetValue("directories.runtimes") ?? ""
    }));

app.MapGet("/api/runtimes", () => Results.Ok(new { runtimes = new[] { "windows", "wsl", "container", "remote" }, executionProviders = new[] { "windows-process", "wsl2" } }));

app.MapGet("/api/engagements", (EngagementService service) => Results.Ok(service.List()));
app.MapPost("/api/engagements", (CreateEngagementRequest request, EngagementService service) => {
    if (string.IsNullOrWhiteSpace(request.Name) || request.Targets.Count == 0) return Results.BadRequest("Name and at least one target are required.");
    var engagement = service.Create(request.Name, request.Targets.Select(x => (x.Value, x.Excluded)), request.ExpiresAt);
    return Results.Created($"/api/engagements/{engagement.Id}", engagement);
});

app.MapGet("/api/jobs", (JobScheduler scheduler, PlatformDatabase database) => Results.Ok(new { active = scheduler.List(), history = database.ListJobs() }));
app.MapGet("/api/findings", (FindingStore findings) => Results.Ok(findings.List()));
app.MapPost("/api/findings", (CreateFindingRequest request, FindingStore findings) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description)) return Results.BadRequest("Title and description are required.");
    var finding = new SecurityAuditPlatform.Core.Findings.Finding(Guid.NewGuid(), request.Title, request.Description, request.Severity,
        request.Asset, request.Remediation, request.EvidenceIds ?? [], DateTimeOffset.UtcNow);
    return Results.Created($"/api/findings/{finding.Id}", findings.Save(finding));
});
app.MapGet("/api/reports/current.html", (ReportService reports) => Results.Content(reports.BuildHtml("Security Audit Platform Assessment Report"), "text/html; charset=utf-8"));
app.MapPost("/api/parsers/nmap", (ParseOutputRequest request, NmapXmlParser parser) =>
{
    try { return Results.Ok(parser.Parse(request.Output)); } catch (Exception ex) { return Results.BadRequest(ex.Message); }
});
app.MapPost("/api/parsers/nuclei", (ParseOutputRequest request, NucleiJsonlParser parser) => Results.Ok(parser.Parse(request.Output)));

app.MapGet("/api/jobs/{id:guid}/evidence", (Guid id, ExecutionEvidenceStore evidence) =>
{
    var result = evidence.Get(id);
    return result is null ? Results.NotFound() : Results.Ok(result);
});
app.MapPost("/api/jobs", (CreateJobRequest request, JobScheduler scheduler) => {
    try { return Results.Accepted("/api/jobs", scheduler.Enqueue(request.ModuleId, request.Target, request.EngagementId, request.Confirmed)); }
    catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
    catch (KeyNotFoundException ex) { return Results.NotFound(ex.Message); }
    catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
});

app.MapFallback(async context => { context.Response.ContentType = "text/html; charset=utf-8"; await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html")); });
app.Run();

public sealed record GitHubModuleInspectRequest(string RepositoryUrl);
public sealed record GitHubModuleImportRequest(string RepositoryUrl, string? Revision = null);
public sealed record CreateEngagementRequest(string Name, List<ScopeTargetRequest> Targets, DateTimeOffset? ExpiresAt = null);
public sealed record ScopeTargetRequest(string Value, bool Excluded = false);
public sealed record CreateJobRequest(string ModuleId, Guid EngagementId, string Target, bool Confirmed = false);
public sealed record ParseOutputRequest(string Output);
public sealed record CreateFindingRequest(string Title, string Description, SecurityAuditPlatform.Core.Findings.FindingSeverity Severity, string? Asset = null, string? Remediation = null, List<Guid>? EvidenceIds = null);
