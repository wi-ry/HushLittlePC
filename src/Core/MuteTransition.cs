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
