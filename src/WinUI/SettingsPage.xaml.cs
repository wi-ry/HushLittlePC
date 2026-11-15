using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
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
        IdleTimeoutSlider.Maximum = IdleTimeoutScale.MaxIndex;
        IdleTimeoutSlider.ThumbToolTipValueConverter = new IdleTimeoutToolTipConverter();
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
            // A saved value between steps (e.g. hand-edited) stays as-is until the slider moves.
            IdleTimeoutSlider.Value = IdleTimeoutScale.IndexOf(settings.IdleMinutes);
            ShowIdleTimeout(settings.IdleMinutes);
            EnabledToggle.IsOn = settings.Enabled;
            UnmuteOnActivityToggle.IsOn = settings.UnmuteOnActivity;
            SkipWhileAudioPlayingToggle.IsOn = settings.SkipWhileAudioPlaying;
            MuteOnDeviceChangeToggle.IsOn = settings.MuteOnDeviceChange;
            ShowNotificationToggle.IsOn = settings.ShowNotification;
            RunOnStartupToggle.IsOn = settings.RunOnStartup;
            ShowSettingsOnStartupToggle.IsOn = settings.ShowSettingsOnStartup;
        }
        finally
        {
            _loading = false;
        }
    }

    private void IdleTimeoutSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs args)
    {
        if (_loading || _context is null)
        {
            return;
        }

        var minutes = IdleTimeoutScale.MinutesAt((int)Math.Round(args.NewValue));
        ShowIdleTimeout(minutes);
        if (minutes == _context.Settings.IdleMinutes)
        {
            return;
        }

        _context.Settings.IdleMinutes = minutes;
        SaveSettings();
    }

    private void ShowIdleTimeout(int minutes)
    {
        var text = IdleTimeoutScale.Format(minutes);
        IdleTimeoutText.Text = text;
        // Screen readers announce the formatted time rather than the slider's step index.
        AutomationProperties.SetItemStatus(IdleTimeoutSlider, text);
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
            case "ShowSettingsOnStartup":
                settings.ShowSettingsOnStartup = toggle.IsOn;
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

/// <summary>Shows the slider thumb tooltip as a time ("1 hr 30 min") instead of a step index.</summary>
internal sealed partial class IdleTimeoutToolTipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        IdleTimeoutScale.Format(IdleTimeoutScale.MinutesAt((int)Math.Round(System.Convert.ToDouble(value))));

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
