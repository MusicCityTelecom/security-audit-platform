using System.Collections.Concurrent;
using System.Threading.Channels;
using SecurityAuditPlatform.Core.Execution;
using SecurityAuditPlatform.Core.Jobs;
using SecurityAuditPlatform.Core.Modules;
using SecurityAuditPlatform.Infrastructure.Data;
using SecurityAuditPlatform.Infrastructure.Engagements;
using SecurityAuditPlatform.Infrastructure.Modules;

namespace SecurityAuditPlatform.Infrastructure.Jobs;

public sealed class JobScheduler : BackgroundService
{
    private readonly Channel<Job> _queue = Channel.CreateUnbounded<Job>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();
    private readonly IModuleRegistry _modules;
    private readonly IReadOnlyDictionary<ModuleRuntime, IExecutionProvider> _providers;
    private readonly PlatformDatabase _database;
    private readonly EngagementService _engagements;

    public JobScheduler(IModuleRegistry modules, IEnumerable<IExecutionProvider> providers, PlatformDatabase database, EngagementService engagements)
    {
        _modules = modules; _providers = providers.ToDictionary(x => x.Runtime); _database = database; _engagements = engagements;
    }

    public IReadOnlyList<Job> List() => _jobs.Values.OrderByDescending(x => x.CreatedAt).ToArray();

    public Job Enqueue(string moduleId, string target, Guid engagementId, bool confirmed)
    {
        var module = _modules.Find(moduleId) ?? throw new KeyNotFoundException($"Module '{moduleId}' is not installed.");
        if (module.ValidationIssues.Any(x => x.IsError)) throw new InvalidOperationException("Module is invalid and cannot execute.");
        if (!_engagements.IsAuthorized(engagementId, target)) throw new UnauthorizedAccessException("Target is outside the active engagement scope.");
        if (module.Manifest.NetworkBehavior is NetworkBehavior.Disruptive or NetworkBehavior.Destructive && !confirmed)
            throw new InvalidOperationException("Explicit confirmation is required for this module.");
        var job = new Job(Guid.NewGuid(), moduleId, engagementId, target, JobState.Queued, DateTimeOffset.UtcNow);
        _jobs[job.Id] = job; _database.SaveJob(job); _queue.Writer.TryWrite(job); return job;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var queued in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            var module = _modules.Find(queued.ModuleId);
            if (module is null) continue;
            var running = queued with { State = JobState.Running, StartedAt = DateTimeOffset.UtcNow };
            _jobs[running.Id] = running; _database.SaveJob(running);
            try
            {
                if (!_providers.TryGetValue(module.Manifest.Runtime, out var provider))
                    throw new InvalidOperationException($"No execution provider for runtime {module.Manifest.Runtime}.");

                var definition = module.Manifest.Execution ??
                    new ModuleExecutionDefinition(module.Manifest.Entrypoint, ["{target}"]);
                var args = definition.Arguments.Select(x => x.Replace("{target}", queued.Target, StringComparison.Ordinal)).ToArray();
                var plan = new ModuleExecutionPlan(module.Manifest.Id, module.Manifest.Runtime, definition.Executable,
                    args, module.Directory, module.Manifest.NetworkBehavior, false);
                var result = await provider.ExecuteAsync(new ExecutionRequest(plan.FileName, plan.Arguments, plan.WorkingDirectory,
                    Timeout: TimeSpan.FromSeconds(Math.Clamp(definition.TimeoutSeconds, 1, 86400))), stoppingToken);
                var state = result.Canceled ? JobState.Canceled : result.TimedOut ? JobState.TimedOut :
                    result.ExitCode == 0 ? JobState.Succeeded : JobState.Failed;
                var error = string.IsNullOrWhiteSpace(result.StandardError) ? null : result.StandardError[..Math.Min(result.StandardError.Length, 4000)];
                var completed = running with { State = state, FinishedAt = result.FinishedAt, ExitCode = result.ExitCode, Error = error };
                _jobs[completed.Id] = completed; _database.SaveJob(completed);
            }
            catch (Exception ex)
            {
                var failed = running with { State = JobState.Failed, FinishedAt = DateTimeOffset.UtcNow, Error = ex.Message };
                _jobs[failed.Id] = failed; _database.SaveJob(failed);
            }
        }
    }
}
