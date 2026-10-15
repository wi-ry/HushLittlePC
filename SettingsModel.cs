namespace HushLittlePC;

using System.Text.Json;

public class Settings
{
    public bool Enabled { get; set; } = true;
    public int IdleMinutes { get; set; } = 10;
    public bool UnmuteOnActivity { get; set; } = false;
    public bool SkipWhileAudioPlaying { get; set; } = false;
    public bool MuteOnDeviceChange { get; set; } = false;
    public bool ShowNotification { get; set; } = true;
    public bool RunOnStartup { get; set; } = false;

    public bool ToggleEnabled() => Enabled = !Enabled;
}

static class SettingsJson
{
    public static string Serialize(Settings settings) =>
        JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

    public static Settings? Deserialize(string json) => JsonSerializer.Deserialize<Settings>(json);
}
