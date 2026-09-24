using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SecurityAuditPlatform.Desktop;

public partial class App : Application
{
    private Process? _webProcess;
    internal string BaseUrl { get; private set; } = "http://127.0.0.1:5187";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var port = FindFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        var webExe = Path.Combine(AppContext.BaseDirectory, "SecurityAuditPlatform.Web.exe");
        if (File.Exists(webExe))
        {
            _webProcess = Process.Start(new ProcessStartInfo
            {
                FileName = webExe,
                Arguments = $"--urls {BaseUrl}",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            });
        }
        var window = new MainWindow(BaseUrl);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { if (_webProcess is { HasExited: false }) _webProcess.Kill(true); } catch { }
        _webProcess?.Dispose();
        base.OnExit(e);
    }

    private static int FindFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }
}
