using SecurityAuditPlatform.Core.Modules;
using SecurityAuditPlatform.Infrastructure.Data;
using SecurityAuditPlatform.Infrastructure.Engagements;
using SecurityAuditPlatform.Infrastructure.Execution;
using SecurityAuditPlatform.Infrastructure.Findings;
using SecurityAuditPlatform.Infrastructure.Reports;
using SecurityAuditPlatform.Infrastructure.Parsers;
using SecurityAuditPlatform.Infrastructure.Jobs;
using SecurityAuditPlatform.Infrastructure.Modules;
using SecurityAuditPlatform.Infrastructure.Tools;
using SecurityAuditPlatform.Infrastructure.Audit;
using SecurityAuditPlatform.Infrastructure.Updates;
using SecurityAuditPlatform.Infrastructure.Terminal;
using SecurityAuditPlatform.Infrastructure.Runtimes;
using SecurityAuditPlatform.Infrastructure.Assets;

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
builder.Services.AddSingleton<PlatformDatabase>(_ => { var db = new PlatformDatabase(Path.Combine(dataDirectory, "platform.db")); db.InitializeAssetSchema(); return db; });
builder.Services.AddSingleton<AssetInventoryService>();
builder.Services.AddSingleton<EngagementService>();
builder.Services.AddSingleton<ExecutionEvidenceStore>();
builder.Services.AddSingleton<SecurityAuditPlatform.Infrastructure.Settings.SettingsService>();
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddSingleton<AuditLogService>();
builder.Services.AddSingleton<TerminalSessionManager>();
builder.Services.AddSingleton<WslRuntimeService>();
builder.Services.AddHttpClient<GitHubReleaseUpdateChecker>();
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

app.Use(async (context, next) =>
{
    var remote = context.Connection.RemoteIpAddress;
    if (remote is not null && !System.Net.IPAddress.IsLoopback(remote))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("The local operator API only accepts loopback connections.");
        return;
    }
    await next();
});

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

app.MapPost("/api/settings/apply-directories", (SecurityAuditPlatform.Infrastructure.Settings.SettingsService settings, IModuleRegistry registry, AuditLogService audit) =>
{
    var modules = settings.GetValue("directories.modules");
    if (!string.IsNullOrWhiteSpace(modules))
    {
        try { registry.SetRootDirectory(modules); audit.Write("settings.apply-directories","success",details:new { ModulesDirectory=modules }); }
        catch (Exception ex) { audit.Write("settings.apply-directories","failure",details:new { Error=ex.Message }); return Results.BadRequest(ex.Message); }
    }
    return Results.Ok(new { modulesDirectory = registry.RootDirectory });
});

app.MapGet("/api/settings/directories", (SecurityAuditPlatform.Infrastructure.Settings.SettingsService settings) =>
    Results.Ok(new {
        data = settings.GetValue("directories.data") ?? "",
        modules = settings.GetValue("directories.modules") ?? "",
        evidence = settings.GetValue("directories.evidence") ?? "",
        reports = settings.GetValue("directories.reports") ?? "",
        tools = settings.GetValue("directories.tools") ?? "",
        runtimes = settings.GetValue("directories.runtimes") ?? ""
    }));

app.MapGet("/api/terminals", (TerminalSessionManager terminals) => Results.Ok(terminals.List()));
app.MapPost("/api/terminals", (CreateTerminalRequest request, TerminalSessionManager terminals, AuditLogService audit) =>
{
    try { var session = terminals.Create(request.Kind, request.WorkingDirectory); audit.Write("terminal.create","success",details:session); return Results.Created("/api/terminals/"+session.Id, session); }
    catch (Exception ex) { audit.Write("terminal.create","failure",details:new { request.Kind, Error=ex.Message }); return Results.BadRequest(ex.Message); }
});
app.MapGet("/api/terminals/{id:guid}/output", (Guid id, TerminalSessionManager terminals) =>
{
    try { return Results.Ok(terminals.Read(id)); } catch (KeyNotFoundException ex) { return Results.NotFound(ex.Message); }
});
app.MapPost("/api/terminals/{id:guid}/input", async (Guid id, TerminalInputRequest request, TerminalSessionManager terminals, CancellationToken ct) =>
{
    try { await terminals.WriteAsync(id, request.Input, ct); return Results.NoContent(); } catch (KeyNotFoundException ex) { return Results.NotFound(ex.Message); } catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
});
app.MapDelete("/api/terminals/{id:guid}", (Guid id, TerminalSessionManager terminals, AuditLogService audit) => { terminals.Close(id); audit.Write("terminal.close","success",details:new { id }); return Results.NoContent(); });

app.MapGet("/api/wsl", (WslRuntimeService wsl) => Results.Ok(new { status=wsl.Status(), distributions=wsl.ListDistributions() }));

app.MapGet("/api/assets", (AssetInventoryService assets) => Results.Ok(assets.List()));
app.MapPost("/api/assets", (CreateAssetRequest request, AssetInventoryService assets) =>
{
    try { return Results.Ok(assets.Upsert(request.ToObservation())); }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
});
app.MapPost("/api/assets/import", (ImportAssetsRequest request, AssetInventoryService assets) =>
{
    try { return Results.Ok(new { imported = assets.Import(request.Assets.Select(x => x.ToObservation())) }); }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
});
app.MapPatch("/api/assets/{id:guid}/status", (Guid id, UpdateAssetStatusRequest request, AssetInventoryService assets) =>
    assets.SetStatus(id, request.Status) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/tools", (ToolRegistry tools) => Results.Ok(tools.CheckAll()));
app.MapPost("/api/tools/check", (ToolRegistry tools) => Results.Ok(tools.CheckAll()));
app.MapGet("/api/audit", (AuditLogService audit) => Results.Ok(audit.List()));
app.MapGet("/api/updates/check", async (GitHubReleaseUpdateChecker checker, CancellationToken ct) =>
    Results.Ok(await checker.CheckAsync("MusicCityTelecom", "security-audit-platform", "0.1.0", ct)));

app.MapGet("/api/runtimes", () => Results.Ok(new { runtimes = new[] { "windows", "wsl", "container", "remote" }, executionProviders = new[] { "windows-process", "wsl2" } }));

app.MapGet("/api/engagements", (EngagementService service) => Results.Ok(service.List()));
app.MapPost("/api/engagements", (CreateEngagementRequest request, EngagementService service, AuditLogService audit) => {
    if (string.IsNullOrWhiteSpace(request.Name) || request.Targets.Count == 0) return Results.BadRequest("Name and at least one target are required.");
    var engagement = service.Create(request.Name, request.Targets.Select(x => (x.Value, x.Excluded)), request.ExpiresAt); audit.Write("engagement.create", "success", target: request.Name, details: new { engagement.Id, TargetCount = request.Targets.Count });
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
app.MapPost("/api/parsers/nmap/assets", (ParseOutputRequest request, NmapXmlParser parser, AssetInventoryService assets) =>
{
    try { return Results.Ok(new { imported = assets.Import(parser.ParseAssets(request.Output)) }); }
    catch (Exception ex) { return Results.BadRequest(ex.Message); }
});
app.MapPost("/api/parsers/nuclei", (ParseOutputRequest request, NucleiJsonlParser parser) => Results.Ok(parser.Parse(request.Output)));

app.MapGet("/api/jobs/{id:guid}/evidence", (Guid id, ExecutionEvidenceStore evidence) =>
{
    var result = evidence.Get(id);
    return result is null ? Results.NotFound() : Results.Ok(result);
});
app.MapPost("/api/jobs", (CreateJobRequest request, JobScheduler scheduler, AuditLogService audit) => {
    try { var job = scheduler.Enqueue(request.ModuleId, request.Target, request.EngagementId, request.Confirmed); audit.Write("job.queue", "success", target: request.Target, details: new { job.Id, job.ModuleId, request.Confirmed }); return Results.Accepted("/api/jobs", job); }
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
public sealed record CreateAssetRequest(string Value, SecurityAuditPlatform.Core.Assets.AssetKind Kind, string? Hostname = null, string? OperatingSystem = null, string? Vendor = null, string? MacAddress = null, int? Port = null, string? Protocol = null, string? Service = null, string? Version = null, string? Source = null, Dictionary<string,string>? Attributes = null)
{
    public SecurityAuditPlatform.Core.Assets.AssetObservation ToObservation() => new(Value, Kind, Hostname, OperatingSystem, Vendor, MacAddress, Port, Protocol, Service, Version, Source, Attributes);
}
public sealed record ImportAssetsRequest(List<CreateAssetRequest> Assets);
public sealed record UpdateAssetStatusRequest(SecurityAuditPlatform.Core.Assets.AssetStatus Status);
public sealed record CreateTerminalRequest(SecurityAuditPlatform.Infrastructure.Terminal.TerminalKind Kind, string? WorkingDirectory = null);
public sealed record TerminalInputRequest(string Input);
public sealed record CreateFindingRequest(string Title, string Description, SecurityAuditPlatform.Core.Findings.FindingSeverity Severity, string? Asset = null, string? Remediation = null, List<Guid>? EvidenceIds = null);
