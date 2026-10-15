using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace HushLittlePC;

class TrayApp : ApplicationContext
{
    readonly Settings _settings = Settings.Load();
    readonly AudioController _audio = new();
    readonly NotifyIcon _tray = new();
    readonly Control _ui = new();            // hidden control, used to hop back onto the UI thread
    DeviceWatcher? _watcher;
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    SettingsForm? _form;
    bool _mutedByUs;

    public TrayApp()
    {
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
        _settings.Enabled = !_settings.Enabled;
        _settings.Save();
        RefreshUi();
    }

    /// <summary>The default playback device changed: mute the new one so nothing plays at a surprise volume.
    /// This is deliberately not tracked as "muted by us", so "unmute when I come back" won't undo it.</summary>
    void OnOutputChanged(string deviceId)
    {
        if (!_settings.Enabled || !_settings.MuteOnDeviceChange) return;

        var device = _audio.MuteDeviceById(deviceId);
        if (device is { WasMuted: false } d && _settings.ShowNotification)
            Notifier.Muted($"Muted {d.Name} because the audio output changed.", deviceId);
    }

    void Tick()
    {
        if (!_settings.Enabled) return;

        var idle = IdleTime.Get();
        if (idle >= TimeSpan.FromMinutes(_settings.IdleMinutes))
        {
            if (_mutedByUs || _audio.IsMuted) return;
            if (_settings.SkipWhileAudioPlaying && _audio.IsPlaying) return;

            var device = _audio.MuteDefault();
            _mutedByUs = device is { WasMuted: false };
            if (device is { WasMuted: false } d && _settings.ShowNotification)
                Notifier.Muted($"Muted {d.Name} after {_settings.IdleMinutes} min of inactivity.", d.Id);
        }
        else if (_mutedByUs)
        {
            // User is back
            if (_settings.UnmuteOnActivity && _audio.IsMuted) _audio.SetMute(false);
            _mutedByUs = false;
        }
    }

    void ShowSettings()
    {
        if (_form is { IsDisposed: false }) { _form.Activate(); return; }

        using (_form = new SettingsForm(_settings))
        {
            // Modal, so OK/Cancel set DialogResult and close it. The 1-second idle
            // timer keeps running while it's open.
            if (_form.ShowDialog() == DialogResult.OK)
            {
                _settings.Save();
                StartupManager.Set(_settings.RunOnStartup);
                RefreshUi();
            }
        }
        _form = null;
    }

    void RefreshUi()
    {
        var old = _tray.Icon;
        _tray.Icon = MakeIcon(_settings.Enabled);
        old?.Dispose();
        _tray.Text = _settings.Enabled
            ? $"HushLittlePC: ON ({_settings.IdleMinutes} min)"
            : "HushLittlePC: OFF";
    }

    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);

    internal static Icon MakeIcon(bool enabled)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var color = enabled ? Color.FromArgb(46, 160, 67) : Color.FromArgb(140, 140, 140);
            using var brush = new SolidBrush(color);
            using var pen = new Pen(color, 3f);

            // speaker body
            g.FillPolygon(brush, new[] {
                new Point(3, 12), new Point(10, 12), new Point(18, 5),
                new Point(18, 27), new Point(10, 20), new Point(3, 20) });

            if (enabled)
            {
                g.DrawArc(pen, 14, 9, 10, 14, -60, 120);
                g.DrawArc(pen, 12, 4, 18, 24, -60, 120);
            }
            else
            {
                using var red = new Pen(Color.FromArgb(200, 50, 50), 3.5f);
                g.DrawLine(red, 22, 11, 30, 21);
                g.DrawLine(red, 30, 11, 22, 21);
            }
        }
        IntPtr h = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(h).Clone();
        DestroyIcon(h);
        return icon;
    }

    void ExitApp()
    {
        _timer.Stop();
        _watcher?.Dispose();
        Notifier.ClearAll();
        _ui.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _audio.Dispose();
        Application.Exit();
    }
}
