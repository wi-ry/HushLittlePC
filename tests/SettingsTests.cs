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
    [DataRow(-5, Settings.MinIdleMinutes)]
    [DataRow(0, Settings.MinIdleMinutes)]
    [DataRow(1, 1)]
    [DataRow(1440, 1440)]
    [DataRow(100000, Settings.MaxIdleMinutes)]
    public void IdleMinutes_IsClampedToSupportedRange(int value, int expected)
    {
        var settings = new Settings { IdleMinutes = value };

        Assert.AreEqual(expected, settings.IdleMinutes);
    }

    [TestMethod]
    public void SettingsJson_WhenIdleMinutesIsOutOfRange_ClampsIt()
    {
        var settings = SettingsJson.Deserialize("""{"IdleMinutes":0}""");

        Assert.IsNotNull(settings);
        Assert.AreEqual(Settings.MinIdleMinutes, settings.IdleMinutes);
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

    [TestMethod]
    public void SettingsDraft_WhenSaveFails_KeepsPreviouslySavedSettings()
    {
        var savedSettings = new Settings { IdleMinutes = 10 };
        var draft = new SettingsDraft(savedSettings);
        var candidate = new Settings { IdleMinutes = 20 };

        var wasSaved = draft.TrySave(candidate, _ => false);

        Assert.IsFalse(wasSaved);
        Assert.AreSame(savedSettings, draft.SavedSettings);
    }

    [TestMethod]
    public void SettingsDraft_WhenSaveSucceeds_UpdatesSavedSettings()
    {
        var savedSettings = new Settings { IdleMinutes = 10 };
        var draft = new SettingsDraft(savedSettings);
        var candidate = new Settings { IdleMinutes = 20 };

        var wasSaved = draft.TrySave(candidate, _ => true);

        Assert.IsTrue(wasSaved);
        Assert.AreSame(candidate, draft.SavedSettings);
    }
}
