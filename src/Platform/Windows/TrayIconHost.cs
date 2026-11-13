using System.Drawing;
using System.Windows.Forms;

namespace HushLittlePC;

sealed class TrayIconHost : IDisposable
{
    readonly NotifyIcon _notifyIcon;
    bool _enabled;

    public event Action? ToggleRequested;
    public event Action? MuteNowRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIconHost(bool enabled, string tooltip)
    {
        _enabled = enabled;
        NativeMenu.FollowSystemTheme();

        _notifyIcon = new NotifyIcon
        {
            Icon = TrayIconFactory.MakeIcon(enabled),
            Text = tooltip,
            Visible = true
        };
        _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        _notifyIcon.MouseUp += NotifyIcon_MouseUp;
    }

    public void Update(bool enabled, string tooltip)
    {
        _enabled = enabled;
        var previousIcon = _notifyIcon.Icon;
        _notifyIcon.Icon = TrayIconFactory.MakeIcon(enabled);
        _notifyIcon.Text = tooltip;
        previousIcon?.Dispose();
    }

    private void NotifyIcon_MouseClick(object? sender, MouseEventArgs args)
    {
        if (args.Button == MouseButtons.Left)
        {
            ToggleRequested?.Invoke();
        }
    }

    private void NotifyIcon_MouseUp(object? sender, MouseEventArgs args)
    {
        if (args.Button != MouseButtons.Right)
        {
            return;
        }

        var menu = new NativeMenu();
        menu.Add(1, "Enabled", _enabled);
        menu.Add(2, "Mute now");
        menu.Add(3, "Settings...");
        menu.AddSeparator();
        menu.Add(4, "Exit");

        switch (menu.Show())
        {
            case 1:
                ToggleRequested?.Invoke();
                break;
            case 2:
                MuteNowRequested?.Invoke();
                break;
            case 3:
                SettingsRequested?.Invoke();
                break;
            case 4:
                ExitRequested?.Invoke();
                break;
        }
    }

    public void Dispose()
    {
        _notifyIcon.MouseClick -= NotifyIcon_MouseClick;
        _notifyIcon.MouseUp -= NotifyIcon_MouseUp;
        _notifyIcon.Visible = false;

        var icon = _notifyIcon.Icon;
        _notifyIcon.Dispose();
        icon?.Dispose();
    }
}
