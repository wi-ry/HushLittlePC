using NAudio.CoreAudioApi;

namespace HushLittlePC;

class AudioController : IDisposable
{
    readonly MMDeviceEnumerator _enumerator = new();

    MMDevice? GetDevice()
    {
        try { return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia); }
        catch (Exception exception)
        {
            AppLog.Warning("Unable to get the default playback device.", exception);
            return null;
        }
    }

    public bool IsMuted
    {
        get
        {
            try
            {
                using var device = GetDevice();
                return device?.AudioEndpointVolume.Mute ?? false;
            }
            catch (Exception exception)
            {
                AppLog.Warning("Unable to read the default playback device mute state.", exception);
                return false;
            }
        }
    }

    public void SetMute(bool mute)
    {
        try { using var d = GetDevice(); if (d != null) d.AudioEndpointVolume.Mute = mute; }
        catch (Exception exception)
        {
            AppLog.Warning($"Unable to set mute state to {mute}.", exception);
        }
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
        catch (Exception exception)
        {
            AppLog.Warning("Unable to mute the default playback device.", exception);
            return null;
        }
    }

    /// <summary>Sets mute on a specific playback device by id.</summary>
    public void SetMuteById(string deviceId, bool mute)
    {
        try { using var d = _enumerator.GetDevice(deviceId); d.AudioEndpointVolume.Mute = mute; }
        catch (Exception exception)
        {
            AppLog.Warning($"Unable to set mute state for playback device '{deviceId}'.", exception);
        }
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
        catch (Exception exception)
        {
            AppLog.Warning($"Unable to mute playback device '{deviceId}'.", exception);
            return null;
        }
    }

    /// <summary>Reads the current audio level and identity of the default multimedia playback endpoint.</summary>
    public PlaybackStatus GetPlaybackStatus()
    {
        try
        {
            using var device = GetDevice();
            if (device is null)
                return new PlaybackStatus(false, null, null, null);

            var deviceId = device.ID;
            var deviceName = device.FriendlyName;
            var peakLevel = device.AudioMeterInformation.MasterPeakValue;
            return new PlaybackStatus(true, deviceId, deviceName, peakLevel);
        }
        catch (Exception exception)
        {
            AppLog.Warning("Unable to read the default playback device audio level.", exception);
            return new PlaybackStatus(false, null, null, null);
        }
    }

    public void Dispose() => _enumerator.Dispose();
}

/// <summary>Snapshot of the default playback endpoint's audio meter at one point in time.</summary>
readonly record struct PlaybackStatus(bool Available, string? DeviceId, string? DeviceName, float? PeakLevel)
{
    public bool IsPlaying => PeakLevel is > 0.001f;
}
