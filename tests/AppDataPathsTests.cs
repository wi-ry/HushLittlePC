using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class AppDataPathsTests
{
    [TestMethod]
    public void ResolveDataDirectory_UsesWritablePortableDirectoryAndMigratesExistingFiles()
    {
        using var directory = new TemporaryDirectory();
        var executablePath = Path.Combine(directory.Root, "HushLittlePC.exe");
        var roamingAppData = Path.Combine(directory.Root, "Roaming");
        var localAppData = Path.Combine(directory.Root, "Local");
        var oldSettings = Path.Combine(roamingAppData, "HushLittlePC", "settings.json");
        var oldCoreLog = Path.Combine(localAppData, "HushLittlePC", "app.log");
        var oldWinUiLog = Path.Combine(localAppData, "HushLittlePC", "winui-app.log");

        Directory.CreateDirectory(Path.GetDirectoryName(oldSettings)!);
        Directory.CreateDirectory(Path.GetDirectoryName(oldCoreLog)!);
        File.WriteAllText(oldSettings, """{"IdleMinutes":25}""");
        File.WriteAllText(oldCoreLog, "core log");
        File.WriteAllText(oldWinUiLog, "winui log");

        var result = AppDataPaths.ResolveDataDirectory(executablePath, roamingAppData, localAppData);

        var expectedDirectory = Path.Combine(directory.Root, "Data");
        Assert.AreEqual(expectedDirectory, result);
        Assert.AreEqual("""{"IdleMinutes":25}""", File.ReadAllText(Path.Combine(result, "settings.json")));
        Assert.AreEqual("core log", File.ReadAllText(Path.Combine(result, "app.log")));
        Assert.AreEqual("winui log", File.ReadAllText(Path.Combine(result, "winui-app.log")));
        Assert.IsTrue(File.Exists(oldSettings), "Migration should preserve the original settings file.");
        Assert.IsTrue(File.Exists(oldCoreLog), "Migration should preserve the original log file.");
    }

    [TestMethod]
    public void ResolveDataDirectory_PreservesFilesAlreadyInPortableDirectory()
    {
        using var directory = new TemporaryDirectory();
        var executablePath = Path.Combine(directory.Root, "HushLittlePC.exe");
        var roamingAppData = Path.Combine(directory.Root, "Roaming");
        var localAppData = Path.Combine(directory.Root, "Local");
        var portableSettings = Path.Combine(directory.Root, "Data", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(portableSettings)!);
        File.WriteAllText(portableSettings, "new settings");

        var oldSettings = Path.Combine(roamingAppData, "HushLittlePC", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(oldSettings)!);
        File.WriteAllText(oldSettings, "old settings");

        var result = AppDataPaths.ResolveDataDirectory(executablePath, roamingAppData, localAppData);

        Assert.AreEqual("new settings", File.ReadAllText(Path.Combine(result, "settings.json")));
    }

    [TestMethod]
    public void ResolveDataDirectory_UsesLocalAppDataWhenExecutablePathIsUnavailable()
    {
        using var directory = new TemporaryDirectory();
        var localAppData = Path.Combine(directory.Root, "Local");

        var result = AppDataPaths.ResolveDataDirectory(
            null,
            Path.Combine(directory.Root, "Roaming"),
            localAppData);

        Assert.AreEqual(Path.Combine(localAppData, "HushLittlePC"), result);
        Assert.IsTrue(Directory.Exists(result));
    }

    sealed class TemporaryDirectory : IDisposable
    {
        public string Root { get; } = Path.Combine(
            Path.GetTempPath(), "HushLittlePC.Tests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
