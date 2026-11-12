namespace HushLittlePC;

using System.Text.Json;

public class Settings
{
    public const int MinIdleMinutes = 1;
    public const int MaxIdleMinutes = 1440;   // 24 hours

    int _idleMinutes = 10;

    public bool Enabled { get; set; } = true;

    /// <summary>Minutes of inactivity before muting. Always kept within
    /// <see cref="MinIdleMinutes"/>..<see cref="MaxIdleMinutes"/>, even if a hand-edited settings file says otherwise
    /// (0 would mute on every tick, a negative value would be nonsense).</summary>
    public int IdleMinutes
    {
        get => _idleMinutes;
        set => _idleMinutes = Math.Clamp(value, MinIdleMinutes, MaxIdleMinutes);
    }
    public bool UnmuteOnActivity { get; set; } = false;
    public bool SkipWhileAudioPlaying { get; set; } = false;
    public bool MuteOnDeviceChange { get; set; } = false;
    public bool ShowNotification { get; set; } = true;
    public bool RunOnStartup { get; set; } = false;

    public bool ToggleEnabled() => Enabled = !Enabled;
}

static class SettingsJson
{
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(Settings settings) =>
        JsonSerializer.Serialize(settings, Options);

    public static Settings? Deserialize(string json) => JsonSerializer.Deserialize<Settings>(json);
}
