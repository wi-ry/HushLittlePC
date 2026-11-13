using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace HushLittlePC.WinUI;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();

        var version = typeof(App).Assembly.GetName().Version;
        VersionText.Text = version?.ToString(3) ?? "Development build";
        RuntimeText.Text = Environment.Version.ToString();

        DiagnosticsText.Text =
            $"Application: Hush Little PC{Environment.NewLine}" +
            $"Version: {VersionText.Text}{Environment.NewLine}" +
            $".NET: {RuntimeText.Text}{Environment.NewLine}" +
            $"Operating system: {Environment.OSVersion}";
    }

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs args)
    {
        var package = new DataPackage();
        package.SetText(DiagnosticsText.Text);
        Clipboard.SetContent(package);
    }
}
