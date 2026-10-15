using Microsoft.Toolkit.Uwp.Notifications;

namespace HushLittlePC;

/// <summary>
/// Native Windows notifications (toasts) with an Unmute button.
/// Works for the portable exe (the library registers the app automatically) and for the MSIX build
/// (which declares the activator in AppxManifest.xml).
/// </summary>
static class Notifier
{
    /// <summary>Shows "muted" with an Unmute button that targets the device that was muted.</summary>
    public static void Muted(string message, string deviceId)
    {
        try
        {
            new ToastContentBuilder()
                .AddArgument("deviceId", deviceId)        // top-level args are also returned for button clicks
                .AddText("HushLittlePC")
                .AddText(message)
                .AddAudio(new ToastAudio { Silent = true })  // the device was just muted; a ping would be pointless
                .AddButton(new ToastButton()
                    .SetContent("Unmute")
                    .AddArgument("action", "unmute")
                    .SetBackgroundActivation())              // handle the click without opening any window
                .Show(toast =>
                {
                    toast.Tag = "muted";
                    toast.Group = "hushlittlepc";             // a new mute replaces the previous notification
                    toast.ExpirationTime = DateTimeOffset.Now.AddHours(4);   // don't leave stale Unmute buttons around
                });
        }
        catch { /* notifications are a nicety, never crash the app over them */ }
    }

    /// <summary>Calls <paramref name="unmute"/> (on a background thread) when the user clicks Unmute.</summary>
    public static void OnUnmuteClicked(Action<string> unmute)
    {
        ToastNotificationManagerCompat.OnActivated += e =>
        {
            try
            {
                var args = ToastArguments.Parse(e.Argument);
                if (args.TryGetValue("action", out var action) && action == "unmute" &&
                    args.TryGetValue("deviceId", out var deviceId))
                    unmute(deviceId);
            }
            catch { }
        };
    }

    public static void ClearAll()
    {
        try { ToastNotificationManagerCompat.History.Clear(); } catch { }
    }
}
