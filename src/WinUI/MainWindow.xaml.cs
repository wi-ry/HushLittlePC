using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace HushLittlePC.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly Action _settingsChanged;
    private readonly AppWindow _appWindow;
    private readonly bool _closeToTrayEnabled;
    private bool _allowClose;

    public MainWindow(Settings settings, Action settingsChanged, bool closeToTrayEnabled)
    {
        _settings = settings;
        _settingsChanged = settingsChanged;
        _closeToTrayEnabled = closeToTrayEnabled;

        InitializeComponent();
        Title = "Hush Little PC";

        // Mica is supported on Windows 11. Keep the default window background on Windows 10.
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            SystemBackdrop = new MicaBackdrop();
        }

        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId)
            ?? throw new InvalidOperationException("The WinUI window could not be resolved to an AppWindow.");

        var dpi = GetDpiForWindow(windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        _appWindow.Resize(new SizeInt32
        {
            Width = (int)Math.Round(1000 * scale),
            Height = (int)Math.Round(720 * scale)
        });

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 720;
            presenter.PreferredMinimumHeight = 600;
        }

        _appWindow.Closing += AppWindow_Closing;

        Navigation.SelectedItem = Navigation.MenuItems[0];
        ContentFrame.Content = new SettingsPage(_settings, _settingsChanged);
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr windowHandle);

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
        if (_allowClose || !_closeToTrayEnabled)
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
