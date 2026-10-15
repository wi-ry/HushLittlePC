using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace HushLittlePC;

static class IdleTime
{
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO info);

    public static TimeSpan Get()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
        uint idleMs = unchecked((uint)Environment.TickCount - info.dwTime);
        return TimeSpan.FromMilliseconds(idleMs);
    }
}

/// <summary>Wraps the default playback device. The device is re-queried every call so
/// switching outputs (headphones, HDMI...) is handled automatically.</summary>
class AudioController : IDisposable
{
    readonly MMDeviceEnumerator _enumerator = new();

    MMDevice? GetDevice()
    {
        try { return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia); }
        catch { return null; }
    }

    public bool IsMuted
    {
        get { using var d = GetDevice(); return d?.AudioEndpointVolume.Mute ?? false; }
    }

    public void SetMute(bool mute)
    {
        try { using var d = GetDevice(); if (d != null) d.AudioEndpointVolume.Mute = mute; }
        catch { }
    }

    /// <summary>Mutes the current default playback device and returns its id, name, and prior mute state (null if unavailable).</summary>
    public (string Id, string Name, bool WasMuted)? MuteDefault()
    {
        try
        {
            using var d = GetDevice();
            if (d == null) return null;
            var wasMuted = MuteTransition.MuteIfNeeded(
                () => d.AudioEndpointVolume.Mute,
                mute => d.AudioEndpointVolume.Mute = mute);
            return (d.ID, d.FriendlyName, wasMuted);
        }
        catch { return null; }
    }

    /// <summary>Sets mute on a specific playback device by id.</summary>
    public void SetMuteById(string deviceId, bool mute)
    {
        try { using var d = _enumerator.GetDevice(deviceId); d.AudioEndpointVolume.Mute = mute; }
        catch { }
    }

    /// <summary>Mutes a specific playback device by id and returns its friendly name and prior mute state (null on failure).</summary>
    public (string Name, bool WasMuted)? MuteDeviceById(string deviceId)
    {
        try
        {
            using var d = _enumerator.GetDevice(deviceId);
            var wasMuted = MuteTransition.MuteIfNeeded(
                () => d.AudioEndpointVolume.Mute,
                mute => d.AudioEndpointVolume.Mute = mute);
            return (d.FriendlyName, wasMuted);
        }
        catch { return null; }
    }

    /// <summary>True if the device is currently outputting audible sound.</summary>
    public bool IsPlaying
    {
        get
        {
            try { using var d = GetDevice(); return (d?.AudioMeterInformation.MasterPeakValue ?? 0f) > 0.001f; }
            catch { return false; }
        }
    }

    public void Dispose() => _enumerator.Dispose();
}

/// <summary>Raises <see cref="DefaultOutputChanged"/> when Windows switches the default playback device
/// (Bluetooth connects, USB/HDMI device appears or disappears, the user picks another output...).
/// Events arrive on a COM thread, so subscribers should marshal to the UI thread.</summary>
sealed class DeviceWatcher : IMMNotificationClient, IDisposable
{
    readonly MMDeviceEnumerator _enumerator = new();
    string? _lastId;
    DateTime _lastAt = DateTime.MinValue;

    public event Action<string>? DefaultOutputChanged;

    public DeviceWatcher() => _enumerator.RegisterEndpointNotificationCallback(this);

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        // Windows reports each role separately; Multimedia is the one that matters for normal playback.
        if (flow != DataFlow.Render || role != Role.Multimedia || string.IsNullOrEmpty(defaultDeviceId)) return;

        // Ignore duplicate notifications for the same device in quick succession.
        if (defaultDeviceId == _lastId && DateTime.UtcNow - _lastAt < TimeSpan.FromSeconds(2)) return;
        _lastId = defaultDeviceId;
        _lastAt = DateTime.UtcNow;

        DefaultOutputChanged?.Invoke(defaultDeviceId);
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
    public void OnDeviceAdded(string pwstrDeviceId) { }
    public void OnDeviceRemoved(string deviceId) { }
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    public void Dispose()
    {
        try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { }
        _enumerator.Dispose();
    }
}
