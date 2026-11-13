using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace HushLittlePC.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly Action _settingsChanged;
    private readonly AppWindow _appWindow;
    private bool _allowClose;

    public MainWindow(Settings settings, Action settingsChanged)
    {
        _settings = settings;
        _settingsChanged = settingsChanged;

        InitializeComponent();
        Title = "Hush Little PC";

        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId)
            ?? throw new InvalidOperationException("The WinUI window could not be resolved to an AppWindow.");
        _appWindow.Closing += AppWindow_Closing;

        Navigation.SelectedItem = Navigation.MenuItems[0];
        ContentFrame.Content = new SettingsPage(_settings, _settingsChanged);
    }

    private void Navigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        ContentFrame.Content = item.Tag?.ToString() switch
        {
            "settings" => new SettingsPage(_settings, _settingsChanged),
            "about" => new AboutPage(),
            _ => ContentFrame.Content
        };
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        sender.Hide();
    }

    public void ShowFromTray()
    {
        _appWindow.Show();
        Activate();
    }

    public void ExitApplication()
    {
        _allowClose = true;
        Close();
    }
}
