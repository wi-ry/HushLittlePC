using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class AppDataPathsTests
{
    [TestMethod]
    public void ResolveDataDirectory_UsesLocalAppDataAndMigratesPreviousData()
    {
        using var directory = new TemporaryDirectory();
        var executablePath = Path.Combine(directory.Root, "HushLittlePC.exe");
        var roamingAppData = Path.Combine(directory.Root, "Roaming");
        var localAppData = Path.Combine(directory.Root, "Local");
        var previousData = Path.Combine(directory.Root, "Data");
        var oldSettings = Path.Combine(roamingAppData, "HushLittlePC", "settings.json");

        Directory.CreateDirectory(previousData);
        Directory.CreateDirectory(Path.GetDirectoryName(oldSettings)!);
        File.WriteAllText(Path.Combine(previousData, "settings.json"), """{"IdleMinutes":25}""");
        File.WriteAllText(Path.Combine(previousData, "app.log"), "previous core log");
        File.WriteAllText(Path.Combine(previousData, "winui-app.log"), "previous WinUI log");
        File.WriteAllText(oldSettings, """{"IdleMinutes":30}""");

        var result = AppDataPaths.ResolveDataDirectory(executablePath, roamingAppData, localAppData);

        var expectedDirectory = Path.Combine(localAppData, "HushLittlePC");
        Assert.AreEqual(expectedDirectory, result);
        Assert.AreEqual("""{"IdleMinutes":25}""", File.ReadAllText(Path.Combine(result, "settings.json")));
        Assert.AreEqual("previous core log", File.ReadAllText(Path.Combine(result, "app.log")));
        Assert.AreEqual("previous WinUI log", File.ReadAllText(Path.Combine(result, "winui-app.log")));
        Assert.IsTrue(File.Exists(Path.Combine(previousData, "settings.json")), "Migration should preserve the previous data.");
        Assert.IsTrue(File.Exists(oldSettings), "Migration should preserve the original roaming settings file.");
    }

    [TestMethod]
    public void ResolveDataDirectory_PreservesFilesAlreadyInLocalAppData()
    {
        using var directory = new TemporaryDirectory();
        var executablePath = Path.Combine(directory.Root, "HushLittlePC.exe");
        var roamingAppData = Path.Combine(directory.Root, "Roaming");
        var localAppData = Path.Combine(directory.Root, "Local");
        var settingsPath = Path.Combine(localAppData, "HushLittlePC", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, "current settings");

        var previousSettings = Path.Combine(directory.Root, "Data", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(previousSettings)!);
        File.WriteAllText(previousSettings, "previous settings");

        var result = AppDataPaths.ResolveDataDirectory(executablePath, roamingAppData, localAppData);

        Assert.AreEqual("current settings", File.ReadAllText(Path.Combine(result, "settings.json")));
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
