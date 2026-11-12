namespace HushLittlePC;

enum IdleAction
{
    None,
    Mute,
    Unmute
}

readonly record struct IdleDecision(IdleAction Action, bool ClearMuteOwnership);

static class IdleMutePolicy
{
    public static IdleDecision Evaluate(
        bool enabled,
        TimeSpan idleTime,
        TimeSpan idleThreshold,
        bool mutedByUs,
        bool deviceMuted,
        bool skipWhileAudioPlaying,
        bool audioPlaying,
        bool unmuteOnActivity)
    {
        if (!enabled)
            return new IdleDecision(IdleAction.None, false);

        if (idleTime < idleThreshold)
        {
            if (!mutedByUs)
                return new IdleDecision(IdleAction.None, false);

            return new IdleDecision(
                unmuteOnActivity && deviceMuted ? IdleAction.Unmute : IdleAction.None,
                ClearMuteOwnership: true);
        }

        if (mutedByUs || deviceMuted || (skipWhileAudioPlaying && audioPlaying))
            return new IdleDecision(IdleAction.None, ClearMuteOwnership: false);

        return new IdleDecision(IdleAction.Mute, ClearMuteOwnership: false);
    }

    public static bool ShouldMuteOnOutputChange(bool enabled, bool muteOnDeviceChange) =>
        enabled && muteOnDeviceChange;

    public static bool ShouldNotify(bool showNotification, bool wasAlreadyMuted) =>
        showNotification && !wasAlreadyMuted;
}
