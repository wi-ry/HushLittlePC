namespace HushLittlePC;

/// <summary>Best-effort application diagnostics stored in the user's local app-data folder.</summary>
static class AppLog
{
    static readonly object Sync = new();
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HushLittlePC", "app.log");

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
                CappedLogFile.Append(
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
