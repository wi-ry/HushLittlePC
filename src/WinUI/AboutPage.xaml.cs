using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace HushLittlePC.WinUI;

public sealed partial class AboutPage : Page
{
    private static readonly Uri SourceUri = new("https://github.com/wi-ry/HushLittlePC");

    private readonly DispatcherTimer _copyFeedbackTimer = new() { Interval = TimeSpan.FromSeconds(2) };

    public AboutPage()
    {
        InitializeComponent();

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(iconPath))
        {
            AppIcon.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        }

        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "Development build";
        VersionText.Text = $"Version {version}";
        RuntimeText.Text = Environment.Version.ToString();

        DiagnosticsText.Text =
            $"Application: Hush Little PC{Environment.NewLine}" +
            $"Version: {version}{Environment.NewLine}" +
            $".NET: {RuntimeText.Text}{Environment.NewLine}" +
            $"Operating system: {Environment.OSVersion}";

        _copyFeedbackTimer.Tick += (_, _) =>
        {
            _copyFeedbackTimer.Stop();
            CopyDiagnosticsIcon.Glyph = "";
            CopyDiagnosticsText.Text = "Copy";
        };
        Unloaded += (_, _) => _copyFeedbackTimer.Stop();
    }

    private async void SourceCard_Click(object sender, RoutedEventArgs args) =>
        await Launcher.LaunchUriAsync(SourceUri);

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs args)
    {
        var package = new DataPackage();
        package.SetText(DiagnosticsText.Text);
        Clipboard.SetContent(package);

        // Confirm the copy in place, as Windows Settings does, and announce it to screen readers.
        CopyDiagnosticsIcon.Glyph = "";
        CopyDiagnosticsText.Text = "Copied";
        Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
            .FromElement(CopyDiagnosticsButton)?
            .RaiseNotificationEvent(
                Microsoft.UI.Xaml.Automation.Peers.AutomationNotificationKind.ActionCompleted,
                Microsoft.UI.Xaml.Automation.Peers.AutomationNotificationProcessing.ImportantMostRecent,
                "Diagnostics copied to the clipboard",
                "DiagnosticsCopied");
        _copyFeedbackTimer.Stop();
        _copyFeedbackTimer.Start();
    }
}
