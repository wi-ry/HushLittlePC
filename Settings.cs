using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Windows.ApplicationModel;

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

static class StartupManager
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "HushLittlePC";
    const string StartupTaskId = "HushLittlePCStartup";   // must match AppxManifest.xml
    const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackageFullName(ref uint length, StringBuilder? name);

    /// <summary>True when running from an MSIX package (e.g. the Microsoft Store version).</summary>
    internal static bool IsPackaged
    {
        get { uint len = 0; return GetCurrentPackageFullName(ref len, null) != APPMODEL_ERROR_NO_PACKAGE; }
    }

    public static void Set(bool enable)
    {
        if (IsPackaged) { _ = SetPackagedAsync(enable); return; }

        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        if (enable && Environment.ProcessPath is { } path)
            key.SetValue(ValueName, $"\"{path}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    // Packaged apps can't use the HKCU Run key (writes are redirected); they use the manifest's StartupTask.
    // If the user turned it off in Task Manager > Startup apps, Windows only lets them turn it back on there.
    static async Task SetPackagedAsync(bool enable)
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (enable) await task.RequestEnableAsync();
            else task.Disable();
        }
        catch { /* ignore: startup is a convenience, not critical */ }
    }
}
