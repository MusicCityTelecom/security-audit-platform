using SecurityAuditPlatform.Infrastructure.Settings;

namespace SecurityAuditPlatform.Infrastructure.Tools;

public sealed record ToolDefinition(string Id, string Name, string Executable, string Runtime, string? VersionArgument = "--version", string? PathSettingKey = null, string? Notes = null);
public sealed record ToolStatus(ToolDefinition Tool, bool Available, string? ResolvedPath, string? Version, string? Error);

public sealed class ToolRegistry
{
    private readonly SettingsService _settings;
    private readonly IReadOnlyList<ToolDefinition> _tools =
    [
        new("nmap","Nmap","nmap","wsl",PathSettingKey:"tools.nmap.path"),
        new("nuclei","Nuclei","nuclei","wsl",PathSettingKey:"tools.nuclei.path"),
        new("metasploit","Metasploit","msfconsole","wsl",PathSettingKey:"tools.metasploit.path"),
        new("masscan","Masscan","masscan","wsl",PathSettingKey:"tools.masscan.path"),
        new("aircrack-ng","Aircrack-ng","aircrack-ng","wsl",PathSettingKey:"tools.aircrack.path"),
        new("wifite2","Wifite2","wifite","wsl",PathSettingKey:"tools.wifite.path"),
        new("hashcat","Hashcat","hashcat","wsl",PathSettingKey:"tools.hashcat.path"),
        new("binwalk","Binwalk","binwalk","wsl",PathSettingKey:"tools.binwalk.path"),
        new("tshark","Wireshark/tshark","tshark","windows",PathSettingKey:"tools.tshark.path"),
        new("powershell","PowerShell","powershell.exe","windows")
    ];

    public ToolRegistry(SettingsService settings) => _settings = settings;
    public IReadOnlyList<ToolDefinition> Definitions => _tools;

    public IReadOnlyList<ToolStatus> CheckAll()
        => _tools.Select(Check).ToArray();

    public ToolStatus Check(ToolDefinition tool)
    {
        try
        {
            var configured = tool.PathSettingKey is null ? null : _settings.GetValue(tool.PathSettingKey);
            var resolved = Resolve(configured, tool.Executable); if (tool.Runtime.Equals("wsl", StringComparison.OrdinalIgnoreCase) && resolved is null) resolved = tool.Executable;
            if (resolved is null) return new(tool, false, null, null, "Executable was not found.");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tool.Runtime.Equals("wsl", StringComparison.OrdinalIgnoreCase) ? "wsl.exe" : resolved,
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
            };
            if (tool.Runtime.Equals("wsl", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add(resolved);
                if (!string.IsNullOrWhiteSpace(tool.VersionArgument)) foreach (var arg in tool.VersionArgument.Split(' ', StringSplitOptions.RemoveEmptyEntries)) psi.ArgumentList.Add(arg);
            }
            else if (!string.IsNullOrWhiteSpace(tool.VersionArgument))
            {
                foreach (var arg in tool.VersionArgument.Split(' ', StringSplitOptions.RemoveEmptyEntries)) psi.ArgumentList.Add(arg);
            }
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null) return new(tool, false, resolved, null, "Unable to start process.");
            if (!process.WaitForExit(5000)) { try { process.Kill(true); } catch { } return new(tool, false, resolved, null, "Version command timed out."); }
            var output = process.StandardOutput.ReadToEnd().Trim();
            if (string.IsNullOrWhiteSpace(output)) output = process.StandardError.ReadToEnd().Trim();
            return new(tool, process.ExitCode == 0 || !string.IsNullOrWhiteSpace(output), resolved, output.Split('\n').FirstOrDefault()?.Trim(), process.ExitCode == 0 ? null : "Version command returned a non-zero exit code.");
        }
        catch (Exception ex) { return new(tool, false, null, null, ex.Message); }
    }

    private static string? Resolve(string? configured, string executable)
    {
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return configured;
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var names = OperatingSystem.IsWindows() && !Path.HasExtension(executable) ? new[] { executable, executable + ".exe" } : new[] { executable };
        return paths.SelectMany(p => names.Select(n => Path.Combine(p, n))).FirstOrDefault(File.Exists);
    }
}
