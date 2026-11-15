namespace HushLittlePC;

/// <summary>Best-effort application diagnostics stored in the user's local app-data folder.</summary>
static class AppLog
{
    static readonly object Sync = new();
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HushLittlePC", "app.log");

    const long MaxFileSize = 512 * 1024;

    public static void Error(string message, Exception exception)
    {
        Write($"ERROR {message}{Environment.NewLine}{exception}");
    }

    static void Write(string message)
    {
        try
        {
            lock (Sync)
            {
                var directory = Path.GetDirectoryName(FilePath)!;
                Directory.CreateDirectory(directory);

                if (File.Exists(FilePath) && new FileInfo(FilePath).Length >= MaxFileSize)
                    File.Delete(FilePath);

                File.AppendAllText(
                    FilePath,
                    $"[{DateTimeOffset.UtcNow:O}] {message}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never prevent the application from starting or continuing to run.
        }
    }
}
