
using NAudio.CoreAudioApi;

namespace HushLittlePC;

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
