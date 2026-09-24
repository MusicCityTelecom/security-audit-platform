using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace SecurityAuditPlatform.Desktop;

public partial class MainWindow : Window
{
    private readonly string _baseUrl;

    public MainWindow(string baseUrl)
    {
        InitializeComponent();
        _baseUrl = baseUrl;
        Loaded += OnLoaded;
        Closed += (_, _) => Browser.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Browser.EnsureCoreWebView2Async();
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = true;
            Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Browser.Source = new Uri(_baseUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to initialize the operator console.\n\n{ex.Message}", "Security Audit Platform", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
