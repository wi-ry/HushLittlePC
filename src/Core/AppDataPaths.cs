namespace HushLittlePC;

/// <summary>
/// Resolves the application's writable data directory. Portable builds keep data beside the
/// executable; packaged or non-writable locations fall back to the user's local app-data folder.
/// </summary>
public static class AppDataPaths
{
    private static readonly object Sync = new();
    private static string? _dataDirectory;

    public static string DataDirectory
    {
        get
        {
            EnsureInitialized();
            return _dataDirectory!;
        }
    }

    public static string SettingsFilePath => Path.Combine(DataDirectory, "settings.json");
    public static string CoreLogFilePath => Path.Combine(DataDirectory, "app.log");
    public static string WinUiLogFilePath => Path.Combine(DataDirectory, "winui-app.log");

    /// <summary>Initializes storage before startup logging or settings access.</summary>
    public static void Initialize()
    {
        lock (Sync)
        {
            if (_dataDirectory is not null)
                return;

            _dataDirectory = ResolveDataDirectory(
                Environment.ProcessPath,
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }
    }

    private static void EnsureInitialized()
    {
        if (_dataDirectory is null)
            Initialize();
    }

    internal static string ResolveDataDirectory(
        string? processPath,
        string roamingAppData,
        string localAppData)
    {
        var fallbackDirectory = Path.Combine(localAppData, "HushLittlePC");
        var executableDirectory = Path.GetDirectoryName(processPath ?? string.Empty);
        var portableDirectory = string.IsNullOrWhiteSpace(executableDirectory)
            ? null
            : Path.Combine(executableDirectory, "Data");

        var dataDirectory = portableDirectory is not null && TryCreateWritableDirectory(portableDirectory)
            ? portableDirectory
            : fallbackDirectory;

        Directory.CreateDirectory(dataDirectory);
        MigrateExistingData(roamingAppData, localAppData, dataDirectory);
        return dataDirectory;
    }

    private static bool TryCreateWritableDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}");
            using (new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }

            File.Delete(probePath);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static void MigrateExistingData(
        string roamingAppData,
        string localAppData,
        string destinationDirectory)
    {
        CopyIfMissing(
            Path.Combine(roamingAppData, "HushLittlePC", "settings.json"),
            Path.Combine(destinationDirectory, "settings.json"));

        foreach (var fileName in new[]
        {
            "app.log",
            "app.old.log",
            "winui-app.log",
            "winui-app.old.log"
        })
        {
            CopyIfMissing(
                Path.Combine(localAppData, "HushLittlePC", fileName),
                Path.Combine(destinationDirectory, fileName));
        }
    }

    private static void CopyIfMissing(string sourcePath, string destinationPath)
    {
        try
        {
            if (File.Exists(sourcePath) && !File.Exists(destinationPath))
                File.Copy(sourcePath, destinationPath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Migration is best-effort; existing data remains in its original location if copying fails.
        }
    }
}
