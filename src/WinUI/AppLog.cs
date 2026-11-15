namespace HushLittlePC.WinUI;

/// <summary>Writes best-effort diagnostics for WinUI startup and runtime failures.</summary>
internal static class AppLog
{
    private static readonly object Sync = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HushLittlePC",
        "winui-app.log");

    public static void Info(string message) => Write("INFO", message, null);

    public static void Error(string message, Exception exception) =>
        Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (Sync)
            {
                var directory = Path.GetDirectoryName(LogPath)!;
                Directory.CreateDirectory(directory);

                var entry = $"[{DateTimeOffset.UtcNow:O}] {level} {message}";
                if (exception is not null)
                {
                    entry += Environment.NewLine + exception;
                }

                File.AppendAllText(LogPath, entry + Environment.NewLine + Environment.NewLine);
            }
        }
        catch
        {
            // Diagnostics must never prevent the application from starting or running.
        }
    }
}
