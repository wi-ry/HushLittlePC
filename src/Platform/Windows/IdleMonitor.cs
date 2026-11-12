namespace HushLittlePC;

/// <summary>Applies the idle-muting policy to the current playback device.</summary>
sealed class IdleMonitor(Settings settings, AudioController audio)
{
    bool _mutedByUs;

    public event Action<string, string>? NotificationRequested;

    public void Tick()
    {
        if (!settings.Enabled) return;

        var idle = IdleTime.Get();
        var idleThreshold = TimeSpan.FromMinutes(settings.IdleMinutes);
        var idleReached = idle >= idleThreshold;
        var deviceMuted = (idleReached || (_mutedByUs && settings.UnmuteOnActivity)) && audio.IsMuted;
        var audioPlaying = idleReached && settings.SkipWhileAudioPlaying && audio.IsPlaying;
        var decision = IdleMutePolicy.Evaluate(
            settings.Enabled,
            idle,
            idleThreshold,
            _mutedByUs,
            deviceMuted,
            settings.SkipWhileAudioPlaying,
            audioPlaying,
            settings.UnmuteOnActivity);

        if (decision.ClearMuteOwnership)
            _mutedByUs = false;

        switch (decision.Action)
        {
            case IdleAction.Mute:
                var device = audio.MuteDefault();
                _mutedByUs = device is { WasMuted: false };
                if (device is { WasMuted: false } d &&
                    IdleMutePolicy.ShouldNotify(settings.ShowNotification, d.WasMuted))
                    NotificationRequested?.Invoke($"Muted {d.Name} after {settings.IdleMinutes} min of inactivity.", d.Id);
                break;
            case IdleAction.Unmute:
                audio.SetMute(false);
                break;
        }
    }
}
