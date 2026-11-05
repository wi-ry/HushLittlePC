using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class MuteTransitionTests
{
    [TestMethod]
    public void MuteIfNeeded_WhenAlreadyMuted_DoesNotSetMute()
    {
        var setMuteCalls = 0;

        var wasMuted = MuteTransition.MuteIfNeeded(
            () => true,
            _ => setMuteCalls++);

        Assert.IsTrue(wasMuted);
        Assert.AreEqual(0, setMuteCalls);
    }

    [TestMethod]
    public void MuteIfNeeded_WhenUnmuted_SetsMuteAndReportsTransition()
    {
        var muteValues = new List<bool>();

        var wasMuted = MuteTransition.MuteIfNeeded(
            () => false,
            mute => muteValues.Add(mute));

        Assert.IsFalse(wasMuted);
        CollectionAssert.AreEqual(new[] { true }, muteValues);
    }

    [TestMethod]
    public void MuteOwnership_ClaimTracksDeviceOnlyWhenItWasNotAlreadyMuted()
    {
        var ownership = new MuteOwnership();

        ownership.Claim("device-1", wasAlreadyMuted: false);
        Assert.AreEqual("device-1", ownership.DeviceId);

        ownership.Claim("device-2", wasAlreadyMuted: true);
        Assert.AreEqual("device-1", ownership.DeviceId);
        Assert.IsTrue(ownership.IsOwned);
    }

    [TestMethod]
    public void MuteOwnership_ReleaseReturnsOwnedDeviceIdAndClearsOwnership()
    {
        var ownership = new MuteOwnership();
        ownership.Claim("device-1", wasAlreadyMuted: false);

        var releasedDeviceId = ownership.Release();

        Assert.AreEqual("device-1", releasedDeviceId);
        Assert.IsNull(ownership.DeviceId);
        Assert.IsFalse(ownership.IsOwned);
    }

    [TestMethod]
    public void MuteOwnership_ReleaseByIdDoesNotClearAnotherDevicesOwnership()
    {
        var ownership = new MuteOwnership();
        ownership.Claim("device-1", wasAlreadyMuted: false);

        ownership.Release("device-2");

        Assert.AreEqual("device-1", ownership.DeviceId);
        Assert.IsTrue(ownership.IsOwned);
    }

    [TestMethod]
    public void MuteOwnership_UnmuteIfStillMutedTargetsTheOwnedDeviceAfterOutputSwitch()
    {
        var ownership = new MuteOwnership();
        ownership.Claim("previous-default", wasAlreadyMuted: false);
        var checkedDevices = new List<string>();
        var muteChanges = new List<(string DeviceId, bool Muted)>();

        var didUnmute = ownership.UnmuteIfStillMuted(
            deviceId =>
            {
                checkedDevices.Add(deviceId);
                return true;
            },
            (deviceId, muted) => muteChanges.Add((deviceId, muted)));

        Assert.IsTrue(didUnmute);
        CollectionAssert.AreEqual(new[] { "previous-default" }, checkedDevices);
        CollectionAssert.AreEqual(
            new[] { ("previous-default", false) },
            muteChanges);
        Assert.IsFalse(ownership.IsOwned);
    }

    [TestMethod]
    public void MuteOwnership_UnmuteIfAlreadyUnmutedClearsOwnershipWithoutChangingMute()
    {
        var ownership = new MuteOwnership();
        ownership.Claim("device-1", wasAlreadyMuted: false);
        var setMuteCalls = 0;

        var didUnmute = ownership.UnmuteIfStillMuted(
            _ => false,
            (_, _) => setMuteCalls++);

        Assert.IsFalse(didUnmute);
        Assert.AreEqual(0, setMuteCalls);
        Assert.IsFalse(ownership.IsOwned);
    }
}
