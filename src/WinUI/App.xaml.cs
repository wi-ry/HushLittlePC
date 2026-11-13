using System.Threading;
using Forms = System.Windows.Forms;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace HushLittlePC.WinUI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Mutex? _instanceMutex;
    private DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _timer;
    private Settings? _settings;
    private AudioController? _audio;
    private IdleMonitor? _idleMonitor;
    private DeviceWatcher? _deviceWatcher;
    private Forms.NotifyIcon? _tray;
    private MainWindow? _window;
    private bool _isExiting;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _instanceMutex = new Mutex(
            initiallyOwned: true,
            name: "HushLittlePC_WinUI_SingleInstance",
            createdNew: out bool createdNew);

        if (!createdNew)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
            Exit();
            return;
        }

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        var settings = SettingsStore.Load();
        var audio = new AudioController();
        var idleMonitor = new IdleMonitor(settings, audio);
        var tray = new Forms.NotifyIcon
        {
            Icon = TrayIconFactory.MakeIcon(settings.Enabled),
            Text = settings.Enabled
                ? $"Hush Little PC: ON ({settings.IdleMinutes} min)"
                : "Hush Little PC: OFF",
            Visible = true
        };
        var deviceWatcher = new DeviceWatcher();
        var timer = dispatcherQueue.CreateTimer();

        _dispatcherQueue = dispatcherQueue;
        _settings = settings;
        _audio = audio;
        _idleMonitor = idleMonitor;
        _tray = tray;
        _deviceWatcher = deviceWatcher;
        _timer = timer;

        idleMonitor.NotificationRequested += Notifier.Muted;
        deviceWatcher.DefaultOutputChanged += deviceId =>
            dispatcherQueue.TryEnqueue(() => OnOutputChanged(deviceId));
        Notifier.OnUnmuteClicked(deviceId =>
            dispatcherQueue.TryEnqueue(() => audio.SetMuteById(deviceId, false)));

        NativeMenu.FollowSystemTheme();

        tray.MouseClick += Tray_MouseClick;
        tray.MouseUp += Tray_MouseUp;

        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => idleMonitor.Tick();
        timer.Start();

        StartupManager.Set(settings.RunOnStartup);

        _window = new MainWindow(settings, OnSettingsChanged);
        _window.Activate();
    }

    private void Tray_MouseClick(object? sender, Forms.MouseEventArgs args)
    {
        if (args.Button == Forms.MouseButtons.Left)
        {
            ToggleEnabled();
        }
    }

    private void Tray_MouseUp(object? sender, Forms.MouseEventArgs args)
    {
        if (args.Button == Forms.MouseButtons.Right)
        {
            ShowTrayMenu();
        }
    }

    private void ShowTrayMenu()
    {
        if (_settings is null || _audio is null)
        {
            return;
        }

        var menu = new NativeMenu();
        menu.Add(1, "Enabled", _settings.Enabled);
        menu.Add(2, "Mute now");
        menu.Add(3, "Settings...");
        menu.AddSeparator();
        menu.Add(4, "Exit");

        switch (menu.Show())
        {
            case 1:
                ToggleEnabled();
                break;
            case 2:
                _audio.SetMute(true);
                break;
            case 3:
                _window?.ShowFromTray();
                break;
            case 4:
                ExitApplication();
                break;
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

    private void RefreshTray()
    {
        if (_tray is null)
        {
            return;
        }

        var oldIcon = _tray.Icon;
        _tray.Icon = TrayIconFactory.MakeIcon(_settings?.Enabled == true);
        _tray.Text = GetTrayText();
        oldIcon?.Dispose();
    }

    private void ExitApplication()
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

        if (_tray is not null)
        {
            _tray.Visible = false;
            var oldIcon = _tray.Icon;
            _tray.Dispose();
            oldIcon?.Dispose();
            _tray = null;
        }

        _audio?.Dispose();
        _audio = null;
        _idleMonitor = null;

        _instanceMutex?.Dispose();
        _instanceMutex = null;

        if (_window is not null)
        {
            _window.ExitApplication();
            _window = null;
        }
        else
        {
            Exit();
        }
    }
}
