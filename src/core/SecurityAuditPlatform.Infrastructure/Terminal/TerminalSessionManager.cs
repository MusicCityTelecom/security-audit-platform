using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace SecurityAuditPlatform.Infrastructure.Terminal;

public enum TerminalKind { PowerShell, Cmd, Wsl }

public sealed record TerminalSessionInfo(Guid Id, TerminalKind Kind, string WorkingDirectory, DateTimeOffset CreatedAt, bool Running);
public sealed record TerminalOutput(Guid SessionId, string Output, string Error, DateTimeOffset ReadAt);

public sealed class TerminalSessionManager : IDisposable
{
    private sealed class Session
    {
        public required Guid Id { get; init; }
        public required TerminalKind Kind { get; init; }
        public required Process Process { get; init; }
        public required string WorkingDirectory { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public StringBuilder Output { get; } = new();
        public StringBuilder Error { get; } = new();
        public object Sync { get; } = new();
    }

    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public TerminalSessionInfo Create(TerminalKind kind, string? workingDirectory = null)
    {
        var directory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : Path.GetFullPath(workingDirectory);
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);

        var psi = new ProcessStartInfo { WorkingDirectory = directory, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        switch (kind)
        {
            case TerminalKind.PowerShell:
                psi.FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
                psi.ArgumentList.Add("-NoLogo"); psi.ArgumentList.Add("-NoProfile");
                break;
            case TerminalKind.Cmd:
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("CMD terminals require Windows.");
                psi.FileName = "cmd.exe"; psi.ArgumentList.Add("/Q"); psi.ArgumentList.Add("/K");
                break;
            case TerminalKind.Wsl:
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WSL terminals require Windows.");
                psi.FileName = "wsl.exe"; psi.ArgumentList.Add("--"); psi.ArgumentList.Add("bash"); psi.ArgumentList.Add("--noprofile"); psi.ArgumentList.Add("--norc");
                break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start terminal process.");
        var session = new Session { Id = Guid.NewGuid(), Kind = kind, Process = process, WorkingDirectory = directory, CreatedAt = DateTimeOffset.UtcNow };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (session.Sync) session.Output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (session.Sync) session.Error.AppendLine(e.Data); };
        process.BeginOutputReadLine(); process.BeginErrorReadLine();
        _sessions[session.Id] = session;
        return Info(session);
    }

    public IReadOnlyList<TerminalSessionInfo> List() => _sessions.Values.Select(Info).OrderByDescending(x => x.CreatedAt).ToArray();

    public TerminalOutput Read(Guid id)
    {
        var session = Get(id);
        lock (session.Sync)
        {
            var output = session.Output.ToString(); var error = session.Error.ToString();
            session.Output.Clear(); session.Error.Clear();
            return new TerminalOutput(id, output, error, DateTimeOffset.UtcNow);
        }
    }

    public async Task WriteAsync(Guid id, string input, CancellationToken ct = default)
    {
        if (input.Length > 64 * 1024) throw new ArgumentException("Terminal input exceeds 64 KiB.");
        var session = Get(id);
        await session.Process.StandardInput.WriteAsync(input.AsMemory(), ct);
        await session.Process.StandardInput.FlushAsync(ct);
    }

    public void Close(Guid id)
    {
        if (!_sessions.TryRemove(id, out var session)) return;
        try { if (!session.Process.HasExited) session.Process.Kill(true); } catch { }
        session.Process.Dispose();
    }

    private Session Get(Guid id) => _sessions.TryGetValue(id, out var session) ? session : throw new KeyNotFoundException($"Terminal session {id} was not found.");
    private static TerminalSessionInfo Info(Session s) => new(s.Id, s.Kind, s.WorkingDirectory, s.CreatedAt, !s.Process.HasExited);

    public void Dispose() { foreach (var id in _sessions.Keys) Close(id); }
}
