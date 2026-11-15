namespace HushLittlePC;

/// <summary>Decides whether the settings window opens when the app starts.</summary>
static class LaunchWindowPolicy
{
    /// <param name="isFirstRun">No settings file exists yet.</param>
    /// <param name="showSettingsOnStartup">The user's "Open settings on startup" preference.</param>
    /// <param name="isNotificationActivation">Launched by clicking a notification action (e.g. Unmute).</param>
    /// <param name="trayIconAvailable">The notification-area icon was created.</param>
    public static bool ShouldShowSettings(
        bool isFirstRun,
        bool showSettingsOnStartup,
        bool isNotificationActivation,
        bool trayIconAvailable)
    {
        // Without a tray icon the window is the only way to reach the app.
        if (!trayIconAvailable)
            return true;

        // A notification button handles its action in the background; never pop a window for it.
        if (isNotificationActivation)
            return false;

        // Introduce the app once, then stay in the notification area unless the user opts in.
        return isFirstRun || showSettingsOnStartup;
    }
}
