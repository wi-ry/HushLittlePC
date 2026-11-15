using System.Windows.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Xaml.Controls;

namespace HushLittlePC.WinUI;

/// <summary>Owns the notification-area icon, its context menu, and tray-specific presentation.</summary>
sealed class TrayController : IDisposable
{
    private const int TrayIconSize = 32;

    private readonly Settings _settings;
    private readonly Func<string> _getTrayText;
    private TaskbarIcon? _trayIcon;
    private ToggleMenuFlyoutItem? _enabledMenuItem;

    public bool IsAvailable => _trayIcon is not null;

    public TrayController(
        Settings settings,
        Action toggleEnabled,
        Action muteNow,
        Action showSettings,
        Action exitApplication,
        Func<string> getTrayText)
    {
        _settings = settings;
        _getTrayText = getTrayText;

        var trayIcon = new TaskbarIcon
        {
            ToolTipText = _getTrayText(),
            Icon = CreateTrayIcon(settings.Enabled),
            LeftClickCommand = new RelayCommand(toggleEnabled),
            NoLeftClickDelay = true,
            MenuActivation = PopupActivationMode.RightClick,
            ContextMenuMode = ContextMenuMode.PopupMenu,
            ContextFlyout = CreateTrayMenu(toggleEnabled, muteNow, showSettings, exitApplication)
        };

        try
        {
            trayIcon.ForceCreate(enablesEfficiencyMode: false);
            _trayIcon = trayIcon;
        }
        catch (Exception exception)
        {
            HushLittlePC.AppLog.Error("Unable to create the Windows notification-area icon.", exception);
            trayIcon.Dispose();
        }
    }

    public void Refresh()
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.Icon = CreateTrayIcon(_settings.Enabled);
        _trayIcon.ToolTipText = _getTrayText();
        if (_enabledMenuItem is not null)
        {
            _enabledMenuItem.IsChecked = _settings.Enabled;
        }
    }

    private MenuFlyout CreateTrayMenu(
        Action toggleEnabled,
        Action muteNow,
        Action showSettings,
        Action exitApplication)
    {
        var menu = new MenuFlyout();
        _enabledMenuItem = new ToggleMenuFlyoutItem
        {
            Text = "Enabled",
            IsChecked = _settings.Enabled,
            Command = new RelayCommand(toggleEnabled)
        };
        menu.Items.Add(_enabledMenuItem);
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Mute now",
            Command = new RelayCommand(muteNow)
        });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Settings...",
            Command = new RelayCommand(showSettings)
        });
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Exit",
            Command = new RelayCommand(exitApplication)
        });

        return menu;
    }

    // H.NotifyIcon hands a Win32 HICON to the shell. Passing a BitmapImage makes it decode the
    // file with System.Drawing.Icon, which only accepts .ico data and fails fast on a PNG.
    // Build a real Icon instead. TaskbarIcon takes ownership and disposes the previous Icon
    // when it is replaced, so always hand it a new instance.
    private static System.Drawing.Icon CreateTrayIcon(bool enabled)
    {
        var appIcon = LoadAppIcon();
        if (enabled)
        {
            return appIcon;
        }

        try
        {
            using (appIcon)
            {
                return CreateDisabledIcon(appIcon);
            }
        }
        catch (Exception exception)
        {
            HushLittlePC.AppLog.Error("Unable to create the disabled tray icon. Using the enabled icon.", exception);
            return LoadAppIcon();
        }
    }

    private static System.Drawing.Icon LoadAppIcon() =>
        new(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"), TrayIconSize, TrayIconSize);

    private static System.Drawing.Icon CreateDisabledIcon(System.Drawing.Icon source)
    {
        using var sourceBitmap = source.ToBitmap();
        using var bitmap = new System.Drawing.Bitmap(
            sourceBitmap.Width,
            sourceBitmap.Height,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.DrawImage(sourceBitmap, 0, 0, sourceBitmap.Width, sourceBitmap.Height);

            var lineWidth = Math.Max(3f, sourceBitmap.Width * 0.12f);
            using var pen = new System.Drawing.Pen(System.Drawing.Color.Red, lineWidth)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            graphics.DrawLine(
                pen,
                sourceBitmap.Width * 0.16f,
                sourceBitmap.Height * 0.84f,
                sourceBitmap.Width * 0.84f,
                sourceBitmap.Height * 0.16f);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var borrowed = System.Drawing.Icon.FromHandle(handle);
            return (System.Drawing.Icon)borrowed.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
        _enabledMenuItem = null;
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
