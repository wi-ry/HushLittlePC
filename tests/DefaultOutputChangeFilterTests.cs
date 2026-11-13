using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class DefaultOutputChangeFilterTests
{
    [TestMethod]
    public void ShouldRaise_RejectsNonRenderAndNonMultimediaNotifications()
    {
        var filter = new DefaultOutputChangeFilter();
        var now = DateTime.UtcNow;

        Assert.IsFalse(filter.ShouldRaise(isRenderFlow: false, isMultimediaRole: true, "device-a", now));
        Assert.IsFalse(filter.ShouldRaise(isRenderFlow: true, isMultimediaRole: false, "device-a", now));
        Assert.IsFalse(filter.ShouldRaise(isRenderFlow: true, isMultimediaRole: true, null, now));
        Assert.IsFalse(filter.ShouldRaise(isRenderFlow: true, isMultimediaRole: true, string.Empty, now));
    }

    [TestMethod]
    public void ShouldRaise_AcceptsFirstValidDeviceNotification()
    {
        var filter = new DefaultOutputChangeFilter();

        Assert.IsTrue(filter.ShouldRaise(true, true, "device-a", DateTime.UtcNow));
    }

    [TestMethod]
    public void ShouldRaise_SuppressesSameDeviceWithinDuplicateWindow()
    {
        var filter = new DefaultOutputChangeFilter();
        var now = DateTime.UtcNow;

        Assert.IsTrue(filter.ShouldRaise(true, true, "device-a", now));
        Assert.IsFalse(filter.ShouldRaise(true, true, "device-a", now.AddMilliseconds(1999)));
    }

    [TestMethod]
    public void ShouldRaise_AllowsDifferentDeviceImmediately()
    {
        var filter = new DefaultOutputChangeFilter();
        var now = DateTime.UtcNow;

        Assert.IsTrue(filter.ShouldRaise(true, true, "device-a", now));
        Assert.IsTrue(filter.ShouldRaise(true, true, "device-b", now.AddMilliseconds(10)));
    }

    [TestMethod]
    public void ShouldRaise_AllowsSameDeviceAfterDuplicateWindow()
    {
        var filter = new DefaultOutputChangeFilter();
        var now = DateTime.UtcNow;

        Assert.IsTrue(filter.ShouldRaise(true, true, "device-a", now));
        Assert.IsTrue(filter.ShouldRaise(true, true, "device-a", now.AddSeconds(2)));
    }
}
