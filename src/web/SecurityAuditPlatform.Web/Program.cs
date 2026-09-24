using SecurityAuditPlatform.Core.Modules;
using SecurityAuditPlatform.Infrastructure.Execution;
using SecurityAuditPlatform.Infrastructure.Modules;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ModuleManifestValidator>();
builder.Services.AddSingleton<ModuleJsonStore>();
builder.Services.AddSingleton<ProcessExecutionProvider>();
builder.Services.AddSingleton<WslExecutionProvider>();
var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "security-audit-platform", version = "0.1.0", utc = DateTimeOffset.UtcNow }));
app.MapPost("/api/modules/validate", (ModuleManifest manifest, ModuleManifestValidator validator) => { var issues = validator.Validate(manifest); return Results.Ok(new { valid = issues.All(x => !x.IsError), issues }); });
app.MapPost("/api/modules/serialize", (ModuleManifest manifest, ModuleJsonStore store) => Results.Text(store.Serialize(manifest), "application/json"));
app.MapGet("/api/runtimes", () => Results.Ok(new { runtimes = new[] { "windows", "wsl", "container", "remote" }, executionProviders = new[] { "windows-process", "wsl2" } }));
app.MapFallback(async context => { context.Response.ContentType = "text/html; charset=utf-8"; await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html")); });
app.Run();
