using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace HushLittlePC.WinUI;

public sealed partial class SettingsPage : Page
{
    readonly Settings _settings;
    readonly Action _settingsChanged;
    bool _initializing = true;

    public SettingsPage(Settings settings, Action settingsChanged)
    {
        _settings = settings;
        _settingsChanged = settingsChanged;

        InitializeComponent();

        IdleMinutesNumberBox.Value = _settings.IdleMinutes;
        IdleMinutesSlider.Value = _settings.IdleMinutes;
        EnabledToggle.IsOn = _settings.Enabled;
        UnmuteOnActivityToggle.IsOn = _settings.UnmuteOnActivity;
        SkipWhileAudioPlayingToggle.IsOn = _settings.SkipWhileAudioPlaying;
        MuteOnDeviceChangeToggle.IsOn = _settings.MuteOnDeviceChange;
        ShowNotificationToggle.IsOn = _settings.ShowNotification;
        RunOnStartupToggle.IsOn = _settings.RunOnStartup;

        _initializing = false;
    }

    private void IdleMinutesSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (_initializing)
        {
            return;
        }

        var minutes = (int)Math.Round(args.NewValue);
        _settings.IdleMinutes = minutes;
        IdleMinutesNumberBox.Value = minutes;
        SaveSettings();
    }

    private void IdleMinutesNumberBox_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
    {
        if (_initializing || double.IsNaN(args.NewValue))
        {
            return;
        }

        var minutes = (int)Math.Clamp(
            Math.Round(args.NewValue),
            Settings.MinIdleMinutes,
            Settings.MaxIdleMinutes);

        _settings.IdleMinutes = minutes;
        IdleMinutesSlider.Value = minutes;
        SaveSettings();
    }

    private void Setting_Toggled(object sender, RoutedEventArgs args)
    {
        if (_initializing || sender is not ToggleSwitch toggle)
        {
            return;
        }

        switch (toggle.Tag?.ToString())
        {
            case "Enabled":
                _settings.Enabled = toggle.IsOn;
                break;
            case "UnmuteOnActivity":
                _settings.UnmuteOnActivity = toggle.IsOn;
                break;
            case "SkipWhileAudioPlaying":
                _settings.SkipWhileAudioPlaying = toggle.IsOn;
                break;
            case "MuteOnDeviceChange":
                _settings.MuteOnDeviceChange = toggle.IsOn;
                break;
            case "ShowNotification":
                _settings.ShowNotification = toggle.IsOn;
                break;
            case "RunOnStartup":
                _settings.RunOnStartup = toggle.IsOn;
                break;
            default:
                return;
        }

        SaveSettings();
    }

    private void SaveSettings()
    {
        SettingsStore.Save(_settings);
        _settingsChanged();
    }
}
