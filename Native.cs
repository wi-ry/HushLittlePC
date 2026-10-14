using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

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
