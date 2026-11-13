using System.Threading;
using System.Windows.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
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

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
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
        var settings = SettingsStore.Load();
        var audio = new AudioController();
        var idleMonitor = new IdleMonitor(settings, audio);
        var deviceWatcher = new DeviceWatcher();
        var timer = dispatcherQueue.CreateTimer();

        _settings = settings;
        _audio = audio;
        _deviceWatcher = deviceWatcher;

        // Register notification activation before tray/window initialization so background
        // Unmute actions are not missed while the application is starting.
        Notifier.OnUnmuteClicked(deviceId =>
            dispatcherQueue.TryEnqueue(() => audio.SetMuteById(deviceId, false)));

        var trayIcon = new TaskbarIcon
        {
            ToolTipText = GetTrayText(),
            IconSource = CreateTrayIcon(settings.Enabled),
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

    private static GeneratedIconSource CreateTrayIcon(bool enabled)
    {
        return new GeneratedIconSource
        {
            Text = enabled ? "\uE767" : "\uE74F",
            Foreground = new SolidColorBrush(enabled ? Colors.LimeGreen : Colors.Gray),
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 32,
            FontWeight = FontWeights.SemiBold
        };
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

        _trayIcon.IconSource = CreateTrayIcon(_settings?.Enabled == true);
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
