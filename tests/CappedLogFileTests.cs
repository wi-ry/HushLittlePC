using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class CappedLogFileTests
{
    [TestMethod]
    public void Append_CreatesDirectoryAndFile()
    {
        using var directory = new TemporaryDirectory();

        CappedLogFile.Append(directory.LogPath, "first", maxBytes: 100);

        Assert.AreEqual("first", File.ReadAllText(directory.LogPath));
        Assert.IsFalse(File.Exists(CappedLogFile.OldPath(directory.LogPath)));
    }

    [TestMethod]
    public void Append_BelowCap_KeepsAppending()
    {
        using var directory = new TemporaryDirectory();

        CappedLogFile.Append(directory.LogPath, "a", maxBytes: 100);
        CappedLogFile.Append(directory.LogPath, "b", maxBytes: 100);

        Assert.AreEqual("ab", File.ReadAllText(directory.LogPath));
    }

    [TestMethod]
    public void Append_AtCap_MovesLogAsideAndStartsFresh()
    {
        using var directory = new TemporaryDirectory();
        CappedLogFile.Append(directory.LogPath, new string('x', 10), maxBytes: 10);

        CappedLogFile.Append(directory.LogPath, "new", maxBytes: 10);

        Assert.AreEqual("new", File.ReadAllText(directory.LogPath));
        Assert.AreEqual(new string('x', 10), File.ReadAllText(CappedLogFile.OldPath(directory.LogPath)));
    }

    [TestMethod]
    public void Append_SecondRollover_ReplacesPreviousOldLog()
    {
        using var directory = new TemporaryDirectory();
        CappedLogFile.Append(directory.LogPath, "1111111111", maxBytes: 10);
        CappedLogFile.Append(directory.LogPath, "2222222222", maxBytes: 10);

        CappedLogFile.Append(directory.LogPath, "3", maxBytes: 10);

        Assert.AreEqual("3", File.ReadAllText(directory.LogPath));
        Assert.AreEqual("2222222222", File.ReadAllText(CappedLogFile.OldPath(directory.LogPath)));
    }

    [TestMethod]
    public void OldPath_InsertsOldBeforeExtension()
    {
        Assert.AreEqual(
            Path.Combine("C:", "logs", "winui-app.old.log"),
            CappedLogFile.OldPath(Path.Combine("C:", "logs", "winui-app.log")));
    }

    sealed class TemporaryDirectory : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(
            Path.GetTempPath(), "HushLittlePC.Tests", Guid.NewGuid().ToString("N"));

        public string LogPath => Path.Combine(DirectoryPath, "nested", "test.log");

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
