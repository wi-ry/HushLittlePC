using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace HushLittlePC.WinUI;

/// <summary>State handed to <see cref="SettingsPage"/> through <see cref="Frame.Navigate(Type, object)"/>.</summary>
public sealed record SettingsPageContext(Settings Settings, Action SettingsChanged);

public sealed partial class SettingsPage : Page
{
    private SettingsPageContext? _context;
    private bool _loading;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs args)
    {
        base.OnNavigatedTo(args);
        _context = args.Parameter as SettingsPageContext
            ?? throw new ArgumentException("SettingsPage requires a SettingsPageContext.", nameof(args));
        LoadFromSettings();
    }

    /// <summary>Re-reads the shared settings, e.g. after the tray menu toggles Enabled.</summary>
    public void LoadFromSettings()
    {
        if (_context is null)
        {
            return;
        }

        var settings = _context.Settings;
        _loading = true;
        try
        {
            IdleMinutesNumberBox.Value = settings.IdleMinutes;
            EnabledToggle.IsOn = settings.Enabled;
            UnmuteOnActivityToggle.IsOn = settings.UnmuteOnActivity;
            SkipWhileAudioPlayingToggle.IsOn = settings.SkipWhileAudioPlaying;
            MuteOnDeviceChangeToggle.IsOn = settings.MuteOnDeviceChange;
            ShowNotificationToggle.IsOn = settings.ShowNotification;
            RunOnStartupToggle.IsOn = settings.RunOnStartup;
        }
        finally
        {
            _loading = false;
        }
    }

    private void IdleMinutesNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_loading || _context is null)
        {
            return;
        }

        // Clearing the box yields NaN; restore the current value rather than saving nothing.
        if (double.IsNaN(args.NewValue))
        {
            sender.Value = _context.Settings.IdleMinutes;
            return;
        }

        var minutes = (int)Math.Clamp(
            Math.Round(args.NewValue),
            Settings.MinIdleMinutes,
            Settings.MaxIdleMinutes);

        if (minutes == _context.Settings.IdleMinutes)
        {
            return;
        }

        _context.Settings.IdleMinutes = minutes;
        SaveSettings();
    }

    private void Setting_Toggled(object sender, RoutedEventArgs args)
    {
        if (_loading || _context is null || sender is not ToggleSwitch toggle)
        {
            return;
        }

        var settings = _context.Settings;
        switch (toggle.Tag?.ToString())
        {
            case "Enabled":
                settings.Enabled = toggle.IsOn;
                break;
            case "UnmuteOnActivity":
                settings.UnmuteOnActivity = toggle.IsOn;
                break;
            case "SkipWhileAudioPlaying":
                settings.SkipWhileAudioPlaying = toggle.IsOn;
                break;
            case "MuteOnDeviceChange":
                settings.MuteOnDeviceChange = toggle.IsOn;
                break;
            case "ShowNotification":
                settings.ShowNotification = toggle.IsOn;
                break;
            case "RunOnStartup":
                settings.RunOnStartup = toggle.IsOn;
                break;
            default:
                return;
        }

        SaveSettings();
    }

    private void SaveSettings()
    {
        if (_context is null)
        {
            return;
        }

        SettingsStore.Save(_context.Settings);
        _context.SettingsChanged();
    }
}
