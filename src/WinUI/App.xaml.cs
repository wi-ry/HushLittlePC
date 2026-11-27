using System.Threading;
using AppLog = HushLittlePC.AppLog;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace HushLittlePC.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Mutex? _instanceMutex;
    private DispatcherQueueTimer? _timer;
    private Settings? _settings;
    private AudioController? _audio;
    private DeviceWatcher? _deviceWatcher;
    private TrayController? _trayController;
    private MainWindow? _window;
    private bool _isExiting;

    public App()
    {
        AppDataPaths.Initialize();

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
        // Notification buttons (e.g. Unmute) launch the app with -ToastActivated when it isn't running.
        var isNotificationActivation = Environment.GetCommandLineArgs().Any(argument =>
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
        var isFirstRun = SettingsStore.IsFirstRun();
        var settings = SettingsStore.Load();
        AppLog.Info(
            $"Settings loaded: Enabled={settings.Enabled}, IdleMinutes={settings.IdleMinutes}, " +
            $"SkipWhileAudioPlaying={settings.SkipWhileAudioPlaying}, UnmuteOnActivity={settings.UnmuteOnActivity}, " +
            $"MuteOnDeviceChange={settings.MuteOnDeviceChange}, ShowNotification={settings.ShowNotification}.");

        if (isFirstRun)
        {
            // Persist defaults now so the welcome window appears only once, even if nothing is changed.
            SettingsStore.Save(settings);
        }

        var idleMonitor = new IdleMonitor(settings, audio);
        var deviceWatcher = new DeviceWatcher();
        var timer = dispatcherQueue.CreateTimer();

        _settings = settings;
        _audio = audio;
        _deviceWatcher = deviceWatcher;

        AppLog.Info("Creating notification-area icon.");
        var trayController = new TrayController(
            settings,
            ToggleEnabled,
            idleMonitor.MuteNow,
            ShowSettingsFromTray,
            () => ExitApplication(),
            GetTrayText);
        _trayController = trayController;

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

        // Hush Little PC is a background utility: it starts in the notification area and the
        // settings window opens only on the first run, when the user opts in, or as a fallback
        // when there is no tray icon to reach the app from.
        if (LaunchWindowPolicy.ShouldShowSettings(
                isFirstRun,
                settings.ShowSettingsOnStartup,
                isNotificationActivation,
                trayIconAvailable: trayController.IsAvailable))
        {
            AppLog.Info($"Showing settings window (first run: {isFirstRun}, tray icon: {trayController.IsAvailable}).");
            ShowSettingsFromTray();
        }
        else
        {
            AppLog.Info("Starting in the notification area.");
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

    private void ShowSettingsFromTray()
    {
        if (_settings is null)
        {
            return;
        }

        if (_window is null)
        {
            AppLog.Info("Creating settings window on demand.");
            _window = new MainWindow(_settings, OnSettingsChanged, _trayController?.IsAvailable == true);
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

    private void RefreshTray() => _trayController?.Refresh();

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

        _trayController?.Dispose();
        _trayController = null;

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
}
