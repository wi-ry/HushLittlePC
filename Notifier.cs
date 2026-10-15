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
            var toast = new ToastContentBuilder()
                .AddArgument("deviceId", deviceId)        // top-level args are also returned for button clicks
                .AddText("HushLittlePC")
                .AddText(message)
                .AddAudio(new ToastAudio { Silent = true })  // the device was just muted; a ping would be pointless
                .AddButton(new ToastButton()
                    .SetContent("Unmute")
                    .AddArgument("action", "unmute")
                    .SetBackgroundActivation());             // handle the click without opening any window

            if (GetLogoUri() is { } logo)
                toast.AddAppLogoOverride(logo);              // the large icon on the left of the notification

            toast.Show(t =>
            {
                t.Tag = "muted";
                t.Group = "hushlittlepc";                    // a new mute replaces the previous notification
                t.ExpirationTime = DateTimeOffset.Now.AddHours(4);   // don't leave stale Unmute buttons around
            });
        }
        catch { /* notifications are a nicety, never crash the app over them */ }
    }

    static Uri? _logoUri;

    /// <summary>
    /// Packaged (Store) builds reference the logo inside the package. The portable exe has no package,
    /// so the embedded PNG is copied to %LocalAppData% once and referenced by file path.
    /// </summary>
    static Uri? GetLogoUri()
    {
        if (_logoUri != null) return _logoUri;
        try
        {
            if (StartupManager.IsPackaged)
                return _logoUri = new Uri("ms-appx:///Assets/Square150x150Logo.png");

            using var src = typeof(Notifier).Assembly.GetManifestResourceStream("HushLittlePC.ToastIcon.png");
            if (src == null) return null;

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HushLittlePC");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "toast-icon.png");
            if (!File.Exists(path) || new FileInfo(path).Length != src.Length)
            {
                using var dst = File.Create(path);
                src.CopyTo(dst);
            }
            return _logoUri = new Uri(path);
        }
        catch { return null; }
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
