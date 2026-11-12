using System.Text.Json;
using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class SettingsStoreTests
{
    [TestMethod]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        using var directory = new TemporaryDirectory();
        var errors = new List<Exception>();

        var settings = SettingsStore.Load(directory.SettingsPath, (_, exception) => errors.Add(exception));

        Assert.IsNotNull(settings);
        Assert.IsTrue(settings.Enabled);
        Assert.AreEqual(10, settings.IdleMinutes);
        Assert.IsTrue(settings.ShowNotification);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Save_CreatesDirectoryAndRoundTripsEverySetting()
    {
        using var directory = new TemporaryDirectory();
        var expected = new Settings
        {
            Enabled = false,
            IdleMinutes = 45,
            UnmuteOnActivity = true,
            SkipWhileAudioPlaying = true,
            MuteOnDeviceChange = true,
            ShowNotification = false,
            RunOnStartup = true
        };
        var errors = new List<Exception>();

        SettingsStore.Save(expected, directory.SettingsPath, (_, exception) => errors.Add(exception));

        Assert.IsTrue(File.Exists(directory.SettingsPath));
        Assert.IsFalse(File.Exists(directory.SettingsPath + ".tmp"));
        Assert.AreEqual(0, errors.Count);

        var actual = SettingsStore.Load(directory.SettingsPath, (_, exception) => errors.Add(exception));

        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Enabled, actual.Enabled);
        Assert.AreEqual(expected.IdleMinutes, actual.IdleMinutes);
        Assert.AreEqual(expected.UnmuteOnActivity, actual.UnmuteOnActivity);
        Assert.AreEqual(expected.SkipWhileAudioPlaying, actual.SkipWhileAudioPlaying);
        Assert.AreEqual(expected.MuteOnDeviceChange, actual.MuteOnDeviceChange);
        Assert.AreEqual(expected.ShowNotification, actual.ShowNotification);
        Assert.AreEqual(expected.RunOnStartup, actual.RunOnStartup);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Save_WhenFileAlreadyExists_ReplacesPreviousSettings()
    {
        using var directory = new TemporaryDirectory();
        var errors = new List<Exception>();

        SettingsStore.Save(new Settings { Enabled = true, IdleMinutes = 10 },
            directory.SettingsPath, (_, exception) => errors.Add(exception));
        SettingsStore.Save(new Settings { Enabled = false, IdleMinutes = 30 },
            directory.SettingsPath, (_, exception) => errors.Add(exception));

        var actual = SettingsStore.Load(directory.SettingsPath, (_, exception) => errors.Add(exception));

        Assert.IsNotNull(actual);
        Assert.IsFalse(actual.Enabled);
        Assert.AreEqual(30, actual.IdleMinutes);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Load_WhenFileContainsMalformedJson_ReturnsDefaultsAndReportsError()
    {
        using var directory = new TemporaryDirectory();
        var errors = new List<(string Message, Exception Exception)>();
        Directory.CreateDirectory(Path.GetDirectoryName(directory.SettingsPath)!);
        File.WriteAllText(directory.SettingsPath, "{");

        var settings = SettingsStore.Load(directory.SettingsPath,
            (message, exception) => errors.Add((message, exception)));

        Assert.IsNotNull(settings);
        Assert.IsTrue(settings.Enabled);
        Assert.AreEqual(10, settings.IdleMinutes);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Failed to load settings; defaults will be used.", errors[0].Message);
        Assert.IsTrue(errors[0].Exception is JsonException);
    }

    sealed class TemporaryDirectory : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(
            Path.GetTempPath(), "HushLittlePC.Tests", Guid.NewGuid().ToString("N"));

        public string SettingsPath => Path.Combine(DirectoryPath, "nested", "settings.json");

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
