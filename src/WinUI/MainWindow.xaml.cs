using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
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

        // Draw the Fluent title bar ourselves so the Mica backdrop and the pane toggle
        // extend into the caption area, as in Windows Settings and the WinUI Gallery.
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Mica is supported on Windows 11. Keep the default window background on Windows 10.
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            SystemBackdrop = new MicaBackdrop();
        }

        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId)
            ?? throw new InvalidOperationException("The WinUI window could not be resolved to an AppWindow.");

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(iconPath))
        {
            _appWindow.SetIcon(iconPath);
            AppTitleBarIcon.ImageSource = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        }

        var dpi = GetDpiForWindow(windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        _appWindow.Resize(new SizeInt32
        {
            Width = (int)Math.Round(1000 * scale),
            Height = (int)Math.Round(720 * scale)
        });

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            // AppWindow sizes are in physical pixels.
            presenter.PreferredMinimumWidth = (int)Math.Round(500 * scale);
            presenter.PreferredMinimumHeight = (int)Math.Round(500 * scale);
        }

        _appWindow.Closing += AppWindow_Closing;

        ShowSettingsPage();
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

        var (pageType, parameter) = item.Tag?.ToString() switch
        {
            "settings" => (typeof(SettingsPage), (object?)new SettingsPageContext(_settings, _settingsChanged)),
            "about" => (typeof(AboutPage), null),
            _ => (null, null)
        };

        if (pageType is null || ContentFrame.CurrentSourcePageType == pageType)
        {
            return;
        }

        ContentFrame.Navigate(pageType, parameter, args.RecommendedNavigationTransitionInfo);
        ContentFrame.BackStack.Clear();
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args) =>
        Navigation.IsPaneOpen = !Navigation.IsPaneOpen;

    private void ShowSettingsPage()
    {
        // Selecting the item navigates through Navigation_SelectionChanged.
        Navigation.SelectedItem = Navigation.MenuItems[0];
        if (ContentFrame.Content is SettingsPage page)
        {
            page.LoadFromSettings();
        }
        else
        {
            ContentFrame.Navigate(
                typeof(SettingsPage),
                new SettingsPageContext(_settings, _settingsChanged),
                new SuppressNavigationTransitionInfo());
            ContentFrame.BackStack.Clear();
        }
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

    public void RefreshSettings()
    {
        (ContentFrame.Content as SettingsPage)?.LoadFromSettings();
    }

    public void ShowFromTray()
    {
        // The tray menu can change Enabled while this window is hidden, so reload the
        // settings page from the shared settings instance before showing it.
        ShowSettingsPage();

        _appWindow.Show();
        Activate();
    }

    public void ExitApplication()
    {
        _allowClose = true;
        Close();
    }
}
