using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class LaunchWindowPolicyTests
{
    [TestMethod]
    public void ShouldShowSettings_NormalLaunch_StaysInTray()
    {
        Assert.IsFalse(Decide());
    }

    [TestMethod]
    public void ShouldShowSettings_FirstRun_ShowsWindow()
    {
        Assert.IsTrue(Decide(isFirstRun: true));
    }

    [TestMethod]
    public void ShouldShowSettings_PreferenceEnabled_ShowsWindow()
    {
        Assert.IsTrue(Decide(showSettingsOnStartup: true));
    }

    [TestMethod]
    public void ShouldShowSettings_NotificationActivation_NeverShowsWindow()
    {
        Assert.IsFalse(Decide(isNotificationActivation: true, isFirstRun: true, showSettingsOnStartup: true));
    }

    [TestMethod]
    public void ShouldShowSettings_NoTrayIcon_AlwaysShowsWindow()
    {
        Assert.IsTrue(Decide(trayIconAvailable: false));
        Assert.IsTrue(Decide(trayIconAvailable: false, isNotificationActivation: true));
    }

    static bool Decide(
        bool isFirstRun = false,
        bool showSettingsOnStartup = false,
        bool isNotificationActivation = false,
        bool trayIconAvailable = true) =>
        LaunchWindowPolicy.ShouldShowSettings(isFirstRun, showSettingsOnStartup, isNotificationActivation, trayIconAvailable);
}
