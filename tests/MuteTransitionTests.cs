using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class MuteTransitionTests
{

    [TestMethod]
    public void UpdateOwnershipAfterMute_ClaimsOwnershipWhenItMutesAnUnmutedDevice()
    {
        Assert.IsTrue(MuteTransition.UpdateOwnershipAfterMute(currentlyOwned: false, wasMuted: false));
    }

    [TestMethod]
    public void UpdateOwnershipAfterMute_DoesNotClaimOwnershipOfAnAlreadyMutedDevice()
    {
        Assert.IsFalse(MuteTransition.UpdateOwnershipAfterMute(currentlyOwned: false, wasMuted: true));
    }

    [TestMethod]
    public void UpdateOwnershipAfterMute_PreservesExistingOwnershipWhenAlreadyMuted()
    {
        Assert.IsTrue(MuteTransition.UpdateOwnershipAfterMute(currentlyOwned: true, wasMuted: true));
    }

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
}
