using System.Diagnostics;
using SecurityAuditPlatform.Core.Execution;
using SecurityAuditPlatform.Core.Modules;

namespace SecurityAuditPlatform.Infrastructure.Execution;

public sealed class ProcessExecutionProvider : IExecutionProvider
{
    public string Id => "windows-process";
    public ModuleRuntime Runtime => ModuleRuntime.Windows;

    public async Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FileName)) throw new ArgumentException("Executable is required.", nameof(request));
        var started = DateTimeOffset.UtcNow;
        using var process = new Process { StartInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            WorkingDirectory = string.IsNullOrWhiteSpace(request.WorkingDirectory) ? Environment.CurrentDirectory : request.WorkingDirectory,
            UseShellExecute = false, RedirectStandardOutput = request.CaptureOutput, RedirectStandardError = request.CaptureOutput, CreateNoWindow = true
        }};
        foreach (var arg in request.Arguments) process.StartInfo.ArgumentList.Add(arg);
        if (request.Environment is not null) foreach (var pair in request.Environment) process.StartInfo.Environment[pair.Key] = pair.Value;
        if (!process.Start()) throw new InvalidOperationException($"Failed to start '{request.FileName}'.");
        var stdout = request.CaptureOutput ? process.StandardOutput.ReadToEndAsync(cancellationToken) : Task.FromResult(string.Empty);
        var stderr = request.CaptureOutput ? process.StandardError.ReadToEndAsync(cancellationToken) : Task.FromResult(string.Empty);
        var timeout = request.Timeout ?? TimeSpan.FromMinutes(30);
        var timedOut = false; var canceled = false;
        try { using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); cts.CancelAfter(timeout); await process.WaitForExitAsync(cts.Token); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { timedOut = true; try { process.Kill(true); } catch { } await process.WaitForExitAsync(); }
        catch (OperationCanceledException) { canceled = true; try { process.Kill(true); } catch { } await process.WaitForExitAsync(); }
        return new ExecutionResult(process.ExitCode, await stdout, await stderr, started, DateTimeOffset.UtcNow, timedOut, canceled);
    }
}
