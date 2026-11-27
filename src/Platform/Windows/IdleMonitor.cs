namespace HushLittlePC;

/// <summary>Applies the idle-muting policy to the current playback device.</summary>
sealed class IdleMonitor(Settings settings, AudioController audio)
{
    static readonly TimeSpan DiagnosticInterval = TimeSpan.FromSeconds(30);

    bool _mutedByUs;
    DateTimeOffset _lastIdleDiagnosticAt;
    string? _lastIdleDiagnosticSignature;

    public event Action<string, string>? NotificationRequested;

    /// <summary>Mutes the current default device and claims ownership only when this call changes its state.</summary>
    public void MuteNow()
    {
        var device = audio.MuteDefault();
        if (device is { } result)
            _mutedByUs = MuteTransition.UpdateOwnershipAfterMute(_mutedByUs, result.WasMuted);
    }

    public void Tick()
    {
        if (!settings.Enabled) return;

        var idle = IdleTime.Get();
        var idleThreshold = TimeSpan.FromMinutes(settings.IdleMinutes);
        var idleReached = idle >= idleThreshold;
        var deviceMuted = (idleReached || (_mutedByUs && settings.UnmuteOnActivity)) && audio.IsMuted;

        PlaybackStatus? playbackStatus = null;
        if (idleReached && settings.SkipWhileAudioPlaying)
            playbackStatus = audio.GetPlaybackStatus();

        var audioPlaying = playbackStatus?.IsPlaying == true;
        var decision = IdleMutePolicy.Evaluate(
            settings.Enabled,
            idle,
            idleThreshold,
            _mutedByUs,
            deviceMuted,
            settings.SkipWhileAudioPlaying,
            audioPlaying,
            settings.UnmuteOnActivity);

        if (idleReached)
            LogIdleDiagnostics(idle, idleThreshold, deviceMuted, playbackStatus, decision);

        if (decision.ClearMuteOwnership)
            _mutedByUs = false;

        switch (decision.Action)
        {
            case IdleAction.Mute:
                var device = audio.MuteDefault();
                _mutedByUs = device is { WasMuted: false };
                if (device is { } mutedDevice)
                    AppLog.Info($"Idle mute applied: device='{mutedDevice.Name}' id='{mutedDevice.Id}' wasAlreadyMuted={mutedDevice.WasMuted} muteOwnedByApp={_mutedByUs}.");
                else
                    AppLog.Info("Idle mute requested, but no default playback device was available.");

                if (device is { WasMuted: false } d &&
                    IdleMutePolicy.ShouldNotify(settings.ShowNotification, d.WasMuted))
                    NotificationRequested?.Invoke($"Muted {d.Name} after {settings.IdleMinutes} min of inactivity.", d.Id);
                break;
            case IdleAction.Unmute:
                AppLog.Info("Idle policy requested unmute after user activity.");
                audio.SetMute(false);
                break;
        }
    }

    private void LogIdleDiagnostics(
        TimeSpan idle,
        TimeSpan idleThreshold,
        bool deviceMuted,
        PlaybackStatus? playbackStatus,
        IdleDecision decision)
    {
        var now = DateTimeOffset.UtcNow;
        var audioPlaying = playbackStatus?.IsPlaying == true;
        var signature = string.Join(
            "|",
            settings.SkipWhileAudioPlaying,
            _mutedByUs,
            deviceMuted,
            audioPlaying,
            playbackStatus?.Available,
            playbackStatus?.DeviceId,
            decision.Action);

        if (signature == _lastIdleDiagnosticSignature &&
            _lastIdleDiagnosticAt != default &&
            now - _lastIdleDiagnosticAt < DiagnosticInterval)
        {
            return;
        }

        var audioDetails = playbackStatus is { } status
            ? status.Available
                ? $"available device='{status.DeviceName}' id='{status.DeviceId}' peak={status.PeakLevel:0.000000} playing={status.IsPlaying} threshold=0.001"
                : "unavailable (see preceding audio-device warning for details)"
            : "not checked (Stay unmuted while audio is playing is disabled)";

        AppLog.Info(
            $"Idle decision: idle={idle.TotalSeconds:0.0}s threshold={idleThreshold.TotalSeconds:0.0}s " +
            $"settings[Enabled={settings.Enabled}, IdleMinutes={settings.IdleMinutes}, " +
            $"SkipWhileAudioPlaying={settings.SkipWhileAudioPlaying}, UnmuteOnActivity={settings.UnmuteOnActivity}] " +
            $"mutedByApp={_mutedByUs} deviceMuted={deviceMuted} audioMeter=[{audioDetails}] action={decision.Action}.");

        _lastIdleDiagnosticAt = now;
        _lastIdleDiagnosticSignature = signature;
    }
}
