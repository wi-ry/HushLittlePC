namespace HushLittlePC;

class TrayApp : ApplicationContext
{
    readonly Settings _settings = SettingsStore.Load();
    readonly AudioController _audio = new();
    readonly IdleMonitor _idleMonitor;
    readonly NotifyIcon _tray = new();
    readonly Control _ui = new();            // hidden control, used to hop back onto the UI thread
    DeviceWatcher? _watcher;
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    SettingsForm? _form;

    public TrayApp()
    {
        _idleMonitor = new IdleMonitor(_settings, _audio);
        _idleMonitor.NotificationRequested += (message, deviceId) => Notifier.Muted(message, deviceId);

        NativeMenu.FollowSystemTheme();

        _tray.MouseUp += (_, e) => { if (e.Button == MouseButtons.Right) ShowMenu(); };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) Toggle(); };
        _tray.Visible = true;

        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        _ = _ui.Handle;   // force handle creation so BeginInvoke works from COM callbacks
        // "Unmute" button on the notification (arrives on a background thread)
        Notifier.OnUnmuteClicked(id =>
        {
            if (_ui.IsHandleCreated) _ui.BeginInvoke(() => _audio.SetMuteById(id, false));
        });

        _watcher = new DeviceWatcher();
        _watcher.DefaultOutputChanged += id =>
        {
            if (_ui.IsHandleCreated) _ui.BeginInvoke(() => OnOutputChanged(id));
        };

        StartupManager.Set(_settings.RunOnStartup);
        RefreshUi();
    }

    void ShowMenu()
    {
        var menu = new NativeMenu();
        menu.Add(1, "Enabled", _settings.Enabled);
        menu.Add(2, "Mute now");
        menu.Add(3, "Settings...");
        menu.AddSeparator();
        menu.Add(4, "Exit");

        switch (menu.Show())
        {
            case 1: Toggle(); break;
            case 2: _audio.SetMute(true); break;
            case 3: ShowSettings(); break;
            case 4: ExitApp(); break;
        }
    }

    void Toggle()
    {
        _settings.ToggleEnabled();
        SettingsStore.Save(_settings);
        RefreshUi();
    }

    /// <summary>The default playback device changed: mute the new one so nothing plays at a surprise volume.
    /// This is deliberately not tracked as "muted by us", so "unmute when I come back" won't undo it.</summary>
    void OnOutputChanged(string deviceId)
    {
        if (!IdleMutePolicy.ShouldMuteOnOutputChange(_settings.Enabled, _settings.MuteOnDeviceChange)) return;

        var device = _audio.MuteDeviceById(deviceId);
        if (device is { } d && IdleMutePolicy.ShouldNotify(_settings.ShowNotification, d.WasMuted))
            Notifier.Muted($"Muted {d.Name} because the audio output changed.", deviceId);
    }

    void Tick() => _idleMonitor.Tick();

    void ShowSettings()
    {
        if (_form is { IsDisposed: false }) { _form.Activate(); return; }

        using (_form = new SettingsForm(_settings))
        {
            // Modal, so OK/Cancel set DialogResult and close it. The 1-second idle
            // timer keeps running while it's open.
            if (_form.ShowDialog() == DialogResult.OK)
            {
                SettingsStore.Save(_settings);
                StartupManager.Set(_settings.RunOnStartup);
                RefreshUi();
            }
        }
        _form = null;
    }

    void RefreshUi()
    {
        var old = _tray.Icon;
        _tray.Icon = TrayIconFactory.MakeIcon(_settings.Enabled);
        old?.Dispose();
        _tray.Text = _settings.Enabled
            ? $"Hush Little PC: ON ({_settings.IdleMinutes} min)"
            : "Hush Little PC: OFF";
    }

    void ExitApp()
    {
        _timer.Stop();
        _timer.Dispose();
        _watcher?.Dispose();
        Notifier.ClearAll();
        _ui.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _audio.Dispose();
        Application.Exit();
    }
}
