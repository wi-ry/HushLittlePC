using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class SettingsTests
{
    [TestMethod]
    public void NewSettings_UseDocumentedDefaults()
    {
        var settings = new Settings();

        Assert.IsTrue(settings.Enabled);
        Assert.AreEqual(10, settings.IdleMinutes);
        Assert.IsFalse(settings.UnmuteOnActivity);
        Assert.IsFalse(settings.SkipWhileAudioPlaying);
        Assert.IsFalse(settings.MuteOnDeviceChange);
        Assert.IsTrue(settings.ShowNotification);
        Assert.IsFalse(settings.RunOnStartup);
    }

    [TestMethod]
    public void ToggleEnabled_AlternatesEnabledState()
    {
        var settings = new Settings();

        Assert.IsFalse(settings.ToggleEnabled());
        Assert.IsFalse(settings.Enabled);
        Assert.IsTrue(settings.ToggleEnabled());
        Assert.IsTrue(settings.Enabled);
    }

    [TestMethod]
    public void SettingsJson_RoundTripsEveryOption()
    {
        var expected = new Settings
        {
            Enabled = false,
            IdleMinutes = 1440,
            UnmuteOnActivity = true,
            SkipWhileAudioPlaying = true,
            MuteOnDeviceChange = true,
            ShowNotification = false,
            RunOnStartup = true
        };

        var actual = SettingsJson.Deserialize(SettingsJson.Serialize(expected));

        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Enabled, actual.Enabled);
        Assert.AreEqual(expected.IdleMinutes, actual.IdleMinutes);
        Assert.AreEqual(expected.UnmuteOnActivity, actual.UnmuteOnActivity);
        Assert.AreEqual(expected.SkipWhileAudioPlaying, actual.SkipWhileAudioPlaying);
        Assert.AreEqual(expected.MuteOnDeviceChange, actual.MuteOnDeviceChange);
        Assert.AreEqual(expected.ShowNotification, actual.ShowNotification);
        Assert.AreEqual(expected.RunOnStartup, actual.RunOnStartup);
    }

    [TestMethod]
    public void SettingsJson_WhenFieldsAreMissing_PreservesTheirDefaults()
    {
        var settings = SettingsJson.Deserialize("""{"Enabled":false}""");

        Assert.IsNotNull(settings);
        Assert.IsFalse(settings.Enabled);
        Assert.AreEqual(10, settings.IdleMinutes);
        Assert.IsTrue(settings.ShowNotification);
    }
}
