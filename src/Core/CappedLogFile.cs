namespace HushLittlePC;

/// <summary>
/// Appends to a log file whose size is capped. When the file reaches the cap it is renamed to
/// "&lt;name&gt;.old&lt;ext&gt;" (replacing any previous one) and a fresh file is started, so at most
/// about twice the cap is kept and the entries just before a rollover are not lost.
/// </summary>
static class CappedLogFile
{
    public const long DefaultMaxBytes = 512 * 1024;

    /// <summary>Appends <paramref name="text"/>. Throws on I/O errors; callers decide whether to ignore them.</summary>
    public static void Append(string path, string text, long maxBytes = DefaultMaxBytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var file = new FileInfo(path);
        if (file.Exists && file.Length >= maxBytes)
            File.Move(path, OldPath(path), overwrite: true);

        File.AppendAllText(path, text);
    }

    /// <summary>The file the previous log is kept in, e.g. "app.log" -> "app.old.log".</summary>
    public static string OldPath(string path) =>
        Path.Combine(
            Path.GetDirectoryName(path)!,
            Path.GetFileNameWithoutExtension(path) + ".old" + Path.GetExtension(path));
}
