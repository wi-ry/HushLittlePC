using System.Text.Json;
using Microsoft.Win32;

namespace HushLittlePC;

public class Settings
{
    public bool Enabled { get; set; } = true;
    public int IdleMinutes { get; set; } = 10;
    public bool UnmuteOnActivity { get; set; } = false;
    public bool SkipWhileAudioPlaying { get; set; } = false;
    public bool ShowNotification { get; set; } = true;
    public bool RunOnStartup { get; set; } = false;

    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HushLittlePC", "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch { /* fall back to defaults */ }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}

static class StartupManager
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "HushLittlePC";

    public static void Set(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        if (enable && Environment.ProcessPath is { } path)
            key.SetValue(ValueName, $"\"{path}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
