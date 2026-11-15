using System.Threading;
using System.Windows.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;

namespace HushLittlePC.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Mutex? _instanceMutex;
    private DispatcherQueueTimer? _timer;
    private Settings? _settings;
    private AudioController? _audio;
    private DeviceWatcher? _deviceWatcher;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _window;
    private bool _isExiting;
    private ToggleMenuFlyoutItem? _enabledMenuItem;

    private const int TrayIconSize = 32;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            var exception = eventArgs.ExceptionObject as Exception
                ?? new Exception(eventArgs.ExceptionObject?.ToString() ?? "Unknown unhandled exception.");
            AppLog.Error("Unhandled process exception.", exception);
        };

        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
            AppLog.Error("Unobserved task exception.", eventArgs.Exception);

        UnhandledException += (_, eventArgs) =>
            AppLog.Error("Unhandled WinUI exception.", eventArgs.Exception);

        AppLog.Info("Application constructor entered.");
        InitializeComponent();
        AppLog.Info("Application XAML initialized.");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppLog.Info("OnLaunched entered.");
        try
        {
            OnLaunchedCore(args);
        }
        catch (Exception exception)
        {
            AppLog.Error("Application startup failed.", exception);
            throw;
        }
    }

    private void OnLaunchedCore(LaunchActivatedEventArgs args)
    {
        AppLog.Info("Resolving activation arguments.");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var isStartupTask = activationArgs?.Kind == ExtendedActivationKind.StartupTask;
        var isBackgroundLaunch = isStartupTask || Environment.GetCommandLineArgs().Any(argument =>
            argument.Equals("--background", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("-ToastActivated", StringComparison.OrdinalIgnoreCase));

        AppLog.Info("Acquiring single-instance mutex.");
        _instanceMutex = new Mutex(
            initiallyOwned: true,
            name: "HushLittlePC_SingleInstance",
            createdNew: out bool createdNew);

        if (!createdNew)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
            Exit();
            return;
        }

        AppLog.Info("Initializing UI dispatcher.");
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("The WinUI dispatcher queue is unavailable.");
        AppLog.Info("Initializing audio controller.");
        var audio = new AudioController();

        // Subscribe as early as possible: toast activation is delivered shortly after process launch.
        Notifier.OnUnmuteClicked(deviceId =>
            dispatcherQueue.TryEnqueue(() => audio.SetMuteById(deviceId, false)));

        AppLog.Info("Loading settings and initializing background services.");
        var settings = SettingsStore.Load();
        var idleMonitor = new IdleMonitor(settings, audio);
        var deviceWatcher = new DeviceWatcher();
        var timer = dispatcherQueue.CreateTimer();

        _settings = settings;
        _audio = audio;
        _deviceWatcher = deviceWatcher;

        AppLog.Info("Creating notification-area icon.");
        var trayIcon = new TaskbarIcon
        {
            ToolTipText = GetTrayText(),
            Icon = CreateTrayIcon(settings.Enabled),
            LeftClickCommand = new RelayCommand(ToggleEnabled),
            NoLeftClickDelay = true,
            MenuActivation = PopupActivationMode.RightClick,
            ContextMenuMode = ContextMenuMode.PopupMenu,
            ContextFlyout = CreateTrayMenu()
        };

        try
        {
            trayIcon.ForceCreate(enablesEfficiencyMode: false);
            _trayIcon = trayIcon;
        }
        catch (Exception ex)
        {
            AppLog.Error("Unable to create the Windows notification-area icon.", ex);
            trayIcon.Dispose();
        }

        AppLog.Info("Starting idle monitor and device watcher.");
        idleMonitor.NotificationRequested += Notifier.Muted;
        deviceWatcher.DefaultOutputChanged += deviceId =>
            dispatcherQueue.TryEnqueue(() => OnOutputChanged(deviceId));
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => idleMonitor.Tick();
        timer.Start();
        _timer = timer;

        AppLog.Info("Configuring startup registration.");
        StartupManager.Set(settings.RunOnStartup);
        AppLog.Info("Startup registration configured.");

        // Background launches stay in the notification area. Create the settings window
        // only when the user explicitly opens it from the tray menu.
        if (!isBackgroundLaunch || _trayIcon is null)
        {
            AppLog.Info("Showing settings window for an interactive launch or tray fallback.");
            ShowSettingsFromTray();
        }
    }

    private void ToggleEnabled()
    {
        if (_settings is null)
        {
            return;
        }

        _settings.ToggleEnabled();
        SettingsStore.Save(_settings);
        RefreshTray();
        _window?.RefreshSettings();
    }

    private void OnSettingsChanged()
    {
        if (_settings is null)
        {
            return;
        }

        StartupManager.Set(_settings.RunOnStartup);
        RefreshTray();
    }

    private void OnOutputChanged(string deviceId)
    {
        if (_settings is null || _audio is null)
        {
            return;
        }

        if (!IdleMutePolicy.ShouldMuteOnOutputChange(_settings.Enabled, _settings.MuteOnDeviceChange))
        {
            return;
        }

        var device = _audio.MuteDeviceById(deviceId);
        if (device is { } result &&
            IdleMutePolicy.ShouldNotify(_settings.ShowNotification, result.WasMuted))
        {
            Notifier.Muted($"Muted {result.Name} because the audio output changed.", deviceId);
        }
    }

    private string GetTrayText()
    {
        if (_settings is null || !_settings.Enabled)
        {
            return "Hush Little PC: OFF";
        }

        return $"Hush Little PC: ON ({_settings.IdleMinutes} min)";
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
            AppLog.Error("Unable to create the disabled tray icon. Using the enabled icon.", exception);
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

    private void ShowSettingsFromTray()
    {
        if (_settings is null)
        {
            return;
        }

        if (_window is null)
        {
            AppLog.Info("Creating settings window on demand.");
            _window = new MainWindow(_settings, OnSettingsChanged, _trayIcon is not null);
            _window.Closed += (_, _) =>
            {
                AppLog.Info("Main window Closed event raised.");
                if (!_isExiting)
                {
                    ExitApplication(closeWindow: false);
                }
            };
        }

        _window.ShowFromTray();
    }

    private MenuFlyout CreateTrayMenu()
    {
        var menu = new MenuFlyout();
        _enabledMenuItem = new ToggleMenuFlyoutItem
        {
            Text = "Enabled",
            IsChecked = _settings?.Enabled == true,
            Command = new RelayCommand(ToggleEnabled)
        };
        menu.Items.Add(_enabledMenuItem);
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Mute now",
            Command = new RelayCommand(() => _audio?.SetMute(true))
        });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Settings...",
            Command = new RelayCommand(ShowSettingsFromTray)
        });
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Exit",
            Command = new RelayCommand(() => ExitApplication())
        });

        return menu;
    }

    private void RefreshTray()
    {
        if (_trayIcon is null || _settings is null)
        {
            return;
        }

        _trayIcon.Icon = CreateTrayIcon(_settings.Enabled);
        _trayIcon.ToolTipText = GetTrayText();
        if (_enabledMenuItem is not null)
        {
            _enabledMenuItem.IsChecked = _settings.Enabled;
        }
    }

    private void ExitApplication(bool closeWindow = true)
    {
        AppLog.Info($"ExitApplication called. closeWindow={closeWindow}.");
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _timer?.Stop();
        _timer = null;

        _deviceWatcher?.Dispose();
        _deviceWatcher = null;

        Notifier.ClearAll();

        _trayIcon?.Dispose();
        _trayIcon = null;

        _audio?.Dispose();
        _audio = null;

        _instanceMutex?.Dispose();
        _instanceMutex = null;

        var window = _window;
        _window = null;

        if (closeWindow && window is not null)
        {
            window.ExitApplication();
        }
        else
        {
            Exit();
        }
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
