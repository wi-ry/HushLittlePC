using HushLittlePC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HushLittlePC.Tests;

[TestClass]
public class IdleTimeoutScaleTests
{
    [TestMethod]
    public void Steps_SpanTheFullSettingsRange()
    {
        Assert.AreEqual(Settings.MinIdleMinutes, IdleTimeoutScale.Steps[0]);
        Assert.AreEqual(Settings.MaxIdleMinutes, IdleTimeoutScale.Steps[IdleTimeoutScale.MaxIndex]);
    }

    [TestMethod]
    public void Steps_AreStrictlyAscending()
    {
        for (var i = 1; i < IdleTimeoutScale.Steps.Count; i++)
        {
            Assert.IsTrue(IdleTimeoutScale.Steps[i] > IdleTimeoutScale.Steps[i - 1], $"Step {i} is not ascending.");
        }
    }

    [TestMethod]
    public void IndexOf_ExactStep_RoundTrips()
    {
        foreach (var minutes in IdleTimeoutScale.Steps)
        {
            Assert.AreEqual(minutes, IdleTimeoutScale.MinutesAt(IdleTimeoutScale.IndexOf(minutes)));
        }
    }

    [TestMethod]
    [DataRow(7, 5)]      // closer to 5 than 10
    [DataRow(8, 10)]
    [DataRow(100, 105)]
    [DataRow(5000, 1440)]
    [DataRow(-3, 1)]
    public void IndexOf_OffScaleValue_SnapsToNearestStep(int minutes, int expected)
    {
        Assert.AreEqual(expected, IdleTimeoutScale.MinutesAt(IdleTimeoutScale.IndexOf(minutes)));
    }

    [TestMethod]
    public void MinutesAt_OutOfRangeIndex_Clamps()
    {
        Assert.AreEqual(Settings.MinIdleMinutes, IdleTimeoutScale.MinutesAt(-1));
        Assert.AreEqual(Settings.MaxIdleMinutes, IdleTimeoutScale.MinutesAt(999));
    }

    [TestMethod]
    [DataRow(1, "1 minute")]
    [DataRow(45, "45 minutes")]
    [DataRow(60, "1 hour")]
    [DataRow(90, "1 hr 30 min")]
    [DataRow(1440, "24 hours")]
    public void Format_UsesMinutesOrHoursAndMinutes(int minutes, string expected)
    {
        Assert.AreEqual(expected, IdleTimeoutScale.Format(minutes));
    }
}
