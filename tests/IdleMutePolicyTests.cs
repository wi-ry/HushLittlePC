using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class IdleMutePolicyTests
{
    static readonly TimeSpan Threshold = TimeSpan.FromMinutes(10);

    [TestMethod]
    public void Evaluate_BelowIdleThreshold_DoesNothing()
    {
        var result = Evaluate(idleTime: Threshold - TimeSpan.FromSeconds(1));

        Assert.AreEqual(IdleAction.None, result.Action);
        Assert.IsFalse(result.ClearMuteOwnership);
    }

    [TestMethod]
    public void Evaluate_AtIdleThreshold_Mutes()
    {
        var result = Evaluate(idleTime: Threshold);

        Assert.AreEqual(IdleAction.Mute, result.Action);
    }

    [TestMethod]
    public void Evaluate_WhenDisabled_DoesNothing()
    {
        var result = Evaluate(enabled: false);

        Assert.AreEqual(IdleAction.None, result.Action);
    }

    [TestMethod]
    public void Evaluate_WhenAlreadyMutedByUser_DoesNotClaimOrChangeMute()
    {
        var result = Evaluate(deviceMuted: true);

        Assert.AreEqual(IdleAction.None, result.Action);
        Assert.IsFalse(result.ClearMuteOwnership);
    }

    [TestMethod]
    public void Evaluate_WhenAlreadyMutedByApp_DoesNotRepeatMute()
    {
        var result = Evaluate(mutedByUs: true);

        Assert.AreEqual(IdleAction.None, result.Action);
    }

    [TestMethod]
    public void Evaluate_WhenAudioIsPlayingAndSkipIsEnabled_DoesNotMute()
    {
        var result = Evaluate(skipWhileAudioPlaying: true, audioPlaying: true);

        Assert.AreEqual(IdleAction.None, result.Action);
    }

    [TestMethod]
    public void Evaluate_WhenAudioIsPlayingAndSkipIsDisabled_Mutes()
    {
        var result = Evaluate(skipWhileAudioPlaying: false, audioPlaying: true);

        Assert.AreEqual(IdleAction.Mute, result.Action);
    }

    [TestMethod]
    public void Evaluate_WhenActivityReturnsAndAppOwnsMute_UnmutesAndClearsOwnership()
    {
        var result = Evaluate(
            idleTime: TimeSpan.Zero,
            mutedByUs: true,
            deviceMuted: true,
            unmuteOnActivity: true);

        Assert.AreEqual(IdleAction.Unmute, result.Action);
        Assert.IsTrue(result.ClearMuteOwnership);
    }

    [TestMethod]
    public void Evaluate_WhenActivityReturnsAndUnmuteIsDisabled_OnlyClearsOwnership()
    {
        var result = Evaluate(
            idleTime: TimeSpan.Zero,
            mutedByUs: true,
            deviceMuted: true,
            unmuteOnActivity: false);

        Assert.AreEqual(IdleAction.None, result.Action);
        Assert.IsTrue(result.ClearMuteOwnership);
    }

    [TestMethod]
    public void Evaluate_WhenActivityReturnsAndMuteWasSetManually_DoesNotUnmute()
    {
        var result = Evaluate(
            idleTime: TimeSpan.Zero,
            mutedByUs: false,
            deviceMuted: true,
            unmuteOnActivity: true);

        Assert.AreEqual(IdleAction.None, result.Action);
        Assert.IsFalse(result.ClearMuteOwnership);
    }

    [TestMethod]
    public void ShouldMuteOnOutputChange_RequiresAppEnabledAndOptionEnabled()
    {
        Assert.IsTrue(IdleMutePolicy.ShouldMuteOnOutputChange(enabled: true, muteOnDeviceChange: true));
        Assert.IsFalse(IdleMutePolicy.ShouldMuteOnOutputChange(enabled: false, muteOnDeviceChange: true));
        Assert.IsFalse(IdleMutePolicy.ShouldMuteOnOutputChange(enabled: true, muteOnDeviceChange: false));
    }

    [TestMethod]
    public void ShouldNotify_RequiresNotificationsEnabledAndAnActualMuteTransition()
    {
        Assert.IsTrue(IdleMutePolicy.ShouldNotify(showNotification: true, wasAlreadyMuted: false));
        Assert.IsFalse(IdleMutePolicy.ShouldNotify(showNotification: false, wasAlreadyMuted: false));
        Assert.IsFalse(IdleMutePolicy.ShouldNotify(showNotification: true, wasAlreadyMuted: true));
    }

    static IdleDecision Evaluate(
        bool enabled = true,
        TimeSpan? idleTime = null,
        bool mutedByUs = false,
        bool deviceMuted = false,
        bool skipWhileAudioPlaying = false,
        bool audioPlaying = false,
        bool unmuteOnActivity = false) =>
        IdleMutePolicy.Evaluate(
            enabled,
            idleTime ?? Threshold,
            Threshold,
            mutedByUs,
            deviceMuted,
            skipWhileAudioPlaying,
            audioPlaying,
            unmuteOnActivity);
}
