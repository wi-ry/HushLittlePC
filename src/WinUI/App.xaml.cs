using System.Threading;
using System.Windows.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.ViewManagement;
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
    private DispatcherQueue? _dispatcherQueue;
    private UISettings? _uiSettings;

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
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var isStartupTask = activationArgs?.Kind == ExtendedActivationKind.StartupTask;
        var isBackgroundLaunch = isStartupTask || Environment.GetCommandLineArgs().Any(argument =>
            argument.Equals("--background", StringComparison.OrdinalIgnoreCase) ||
            argument.Equals("-ToastActivated", StringComparison.OrdinalIgnoreCase));

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

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("The WinUI dispatcher queue is unavailable.");
        _dispatcherQueue = dispatcherQueue;
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;

        var audio = new AudioController();

        // Subscribe as early as possible: toast activation is delivered shortly after process launch.
        Notifier.OnUnmuteClicked(deviceId =>
            dispatcherQueue.TryEnqueue(() => audio.SetMuteById(deviceId, false)));

        var settings = SettingsStore.Load();
        var idleMonitor = new IdleMonitor(settings, audio);
        var deviceWatcher = new DeviceWatcher();
        var timer = dispatcherQueue.CreateTimer();

        _settings = settings;
        _audio = audio;
        _deviceWatcher = deviceWatcher;

        var trayIcon = new TaskbarIcon
        {
            ToolTipText = GetTrayText(),
            IconSource = CreateTrayIcon(),
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

        idleMonitor.NotificationRequested += Notifier.Muted;
        deviceWatcher.DefaultOutputChanged += deviceId =>
            dispatcherQueue.TryEnqueue(() => OnOutputChanged(deviceId));
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => idleMonitor.Tick();
        timer.Start();
        _timer = timer;

        StartupManager.Set(settings.RunOnStartup);

        _window = new MainWindow(settings, OnSettingsChanged, _trayIcon is not null);
        _window.Closed += (_, _) =>
        {
            if (!_isExiting)
            {
                ExitApplication(closeWindow: false);
            }
        };
        // Never leave the user with an invisible process if tray-icon creation failed.
        if (!isBackgroundLaunch || _trayIcon is null)
        {
            _window.Activate();
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
        if (_settings?.Enabled != true)
        {
            return "Hush Little PC — Disabled";
        }

        return $"Hush Little PC — Enabled ({_settings.IdleMinutes} min)";
    }

    private BitmapImage CreateTrayIcon()
    {
        var theme = IsLightTaskbarTheme() ? "light" : "dark";
        var state = _settings?.Enabled == true ? "enabled" : "disabled";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Tray", $"{theme}-{state}.png");
        if (!File.Exists(iconPath))
        {
            throw new FileNotFoundException("The Hush Little PC tray icon asset was not found.", iconPath);
        }

        return new BitmapImage
        {
            UriSource = new Uri(iconPath, UriKind.Absolute)
        };
    }

    private bool IsLightTaskbarTheme()
    {
        var background = _uiSettings?.GetColorValue(UIColorType.Background)
            ?? Windows.UI.Color.FromArgb(255, 32, 32, 32);
        var brightness = (0.2126 * background.R) + (0.7152 * background.G) + (0.0722 * background.B);
        return brightness >= 128;
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        _dispatcherQueue?.TryEnqueue(() => RefreshTray());
    }

    private MenuFlyout CreateTrayMenu()
    {
        var menu = new MenuFlyout();
        menu.Items.Add(new ToggleMenuFlyoutItem
        {
            Text = "Enabled",
            IsChecked = _settings?.Enabled == true,
            Command = new RelayCommand(ToggleEnabled)
        });
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Mute now",
            Command = new RelayCommand(() => _audio?.SetMute(true))
        });
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(new MenuFlyoutItem
        {
            Text = "Settings...",
            Command = new RelayCommand(() => _window?.ShowFromTray())
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
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.IconSource = CreateTrayIcon();
        _trayIcon.ToolTipText = GetTrayText();
        _trayIcon.ContextFlyout = CreateTrayMenu();
    }

    private void ExitApplication(bool closeWindow = true)
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _timer?.Stop();
        _timer = null;

        _deviceWatcher?.Dispose();
        _deviceWatcher = null;

        if (_uiSettings is not null)
        {
            _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
            _uiSettings = null;
        }

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
