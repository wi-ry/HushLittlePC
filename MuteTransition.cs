namespace HushLittlePC;

static class MuteTransition
{
    public static bool MuteIfNeeded(Func<bool> isMuted, Action<bool> setMute)
    {
        var wasMuted = isMuted();
        if (!wasMuted) setMute(true);
        return wasMuted;
    }
}

sealed class MuteOwnership
{
    public string? DeviceId { get; private set; }
    public bool IsOwned => DeviceId is not null;

    public void Claim(string deviceId, bool wasAlreadyMuted)
    {
        if (!wasAlreadyMuted)
            DeviceId = deviceId;
    }

    public string? Release()
    {
        var deviceId = DeviceId;
        DeviceId = null;
        return deviceId;
    }

    public void Release(string deviceId)
    {
        if (DeviceId == deviceId)
            DeviceId = null;
    }

    public bool UnmuteIfStillMuted(Func<string, bool> isMuted, Action<string, bool> setMute)
    {
        var deviceId = Release();
        if (deviceId is null || !isMuted(deviceId))
            return false;

        setMute(deviceId, false);
        return true;
    }
}
