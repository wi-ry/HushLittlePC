using System.Text.Json;

namespace HushLittlePC;

/// <summary>Loads and atomically saves the user's application settings.</summary>
static class SettingsStore
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HushLittlePC", "settings.json");

    public static Settings Load() => Load(FilePath, AppLog.Error);

    internal static Settings Load(string filePath, Action<string, Exception> logError)
    {
        try
        {
            if (File.Exists(filePath))
                return SettingsJson.Deserialize(File.ReadAllText(filePath)) ?? new Settings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logError("Failed to load settings; defaults will be used.", ex);
        }

        return new Settings();
    }

    public static void Save(Settings settings) => Save(settings, FilePath, AppLog.Error);

    internal static void Save(Settings settings, string filePath, Action<string, Exception> logError)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            // Write to a temp file and swap it in, so a crash mid-write can't leave a truncated settings.json.
            var tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, SettingsJson.Serialize(settings));
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logError("Failed to save settings.", ex);
        }
    }
}
