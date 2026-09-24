using System.Diagnostics;

namespace SecurityAuditPlatform.Infrastructure.Runtimes;

public sealed record WslDistribution(string Name, bool Default, string State, string Version);
public sealed class WslRuntimeService
{
    public IReadOnlyList<WslDistribution> ListDistributions()
    {
        if (!OperatingSystem.IsWindows()) return [];
        var psi=new ProcessStartInfo("wsl.exe","--list --verbose"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
        using var p=Process.Start(psi); if(p is null)return [];
        var output=p.StandardOutput.ReadToEnd(); p.WaitForExit(5000);
        var result=new List<WslDistribution>();
        foreach(var line in output.Split('\n',StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var normalized=line.Replace("\0","").Trim();
            if(string.IsNullOrWhiteSpace(normalized))continue;
            var isDefault=normalized.StartsWith("*",StringComparison.Ordinal); normalized=normalized.TrimStart('*',' ','\t');
            var parts=System.Text.RegularExpressions.Regex.Split(normalized,"\\s{2,}");
            if(parts.Length>=3)result.Add(new WslDistribution(parts[0].Trim(),isDefault,parts[1].Trim(),parts[2].Trim()));
        }
        return result;
    }

    public string? Status()
    {
        if(!OperatingSystem.IsWindows())return "WSL requires Windows.";
        var psi=new ProcessStartInfo("wsl.exe","--status"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
        using var p=Process.Start(psi); if(p is null)return null;
        var output=p.StandardOutput.ReadToEnd(); p.WaitForExit(5000);
        return string.IsNullOrWhiteSpace(output)?p.StandardError.ReadToEnd():output;
    }
}
