namespace HushLittlePC;

/// <summary>Best-effort application diagnostics stored in the user's local app-data folder.</summary>
public static class AppLog
{
    private static readonly object Sync = new();
    private static string FilePath => AppDataPaths.CoreLogFilePath;

    public static void Info(string message) => Write("INFO", message, null);

    public static void Warning(string message, Exception exception) =>
        Write("WARN", message, exception);

    public static void Error(string message, Exception exception) =>
        Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (Sync)
            {
                var entry = $"[{DateTimeOffset.UtcNow:O}] {level} {message}";
                if (exception is not null)
                    entry += Environment.NewLine + exception;

                CappedLogFile.Append(
                    FilePath,
                    entry + Environment.NewLine + Environment.NewLine);
            }
        }
        catch
        {
            // Diagnostics must never prevent the application from starting or continuing to run.
        }
    }
}
