using SecurityAuditPlatform.Core.Execution;
using SecurityAuditPlatform.Core.Modules;

namespace SecurityAuditPlatform.Infrastructure.Execution;

public sealed class WslExecutionProvider(ProcessExecutionProvider process, string distribution = "") : IExecutionProvider
{
    public string Id => "wsl2";
    public ModuleRuntime Runtime => ModuleRuntime.Wsl;
    public Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var args = new List<string>();
        if (!string.IsNullOrWhiteSpace(distribution)) { args.Add("-d"); args.Add(distribution); }
        args.Add("--"); args.Add(request.FileName); args.AddRange(request.Arguments);
        return process.ExecuteAsync(request with { FileName = "wsl.exe", Arguments = args }, cancellationToken);
    }
}
