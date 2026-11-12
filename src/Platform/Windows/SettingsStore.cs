
using System.Text.Json;

namespace HushLittlePC;

static class SettingsStore
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HushLittlePC", "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return SettingsJson.Deserialize(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Unreadable or corrupt settings file: fall back to defaults.
        }
        return new Settings();
    }

    public static void Save(Settings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            // Write to a temp file and swap it in, so a crash mid-write can't leave a truncated settings.json.
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, SettingsJson.Serialize(settings));
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience; failing to persist them must not crash the tray app.
        }
    }
}

