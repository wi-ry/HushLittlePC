namespace HushLittlePC;

static class MuteTransition
{
    public static bool UpdateOwnershipAfterMute(bool currentlyOwned, bool wasMuted) =>
        currentlyOwned || !wasMuted;

    public static bool MuteIfNeeded(Func<bool> isMuted, Action<bool> setMute)
    {
        var wasMuted = isMuted();
        if (!wasMuted) setMute(true);
        return wasMuted;
    }
}
