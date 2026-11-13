using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class ToastActivationPolicyTests
{
    [TestMethod]
    public void TryGetUnmuteDeviceId_RequiresUnmuteActionAndDeviceId()
    {
        Assert.IsTrue(ToastActivationPolicy.TryGetUnmuteDeviceId("unmute", "device-a", out var deviceId));
        Assert.AreEqual("device-a", deviceId);

        Assert.IsFalse(ToastActivationPolicy.TryGetUnmuteDeviceId("open", "device-a", out _));
        Assert.IsFalse(ToastActivationPolicy.TryGetUnmuteDeviceId(null, "device-a", out _));
        Assert.IsFalse(ToastActivationPolicy.TryGetUnmuteDeviceId("unmute", null, out _));
        Assert.IsFalse(ToastActivationPolicy.TryGetUnmuteDeviceId("unmute", "", out _));
        Assert.IsFalse(ToastActivationPolicy.TryGetUnmuteDeviceId("unmute", "  ", out _));
    }
}
