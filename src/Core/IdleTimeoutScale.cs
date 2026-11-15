namespace HushLittlePC;

/// <summary>
/// Maps the idle-timeout slider to a stepped scale. A linear 1-1440 minute slider makes
/// short timeouts impossible to pick precisely, so steps are fine at the low end and
/// coarse at the high end.
/// </summary>
public static class IdleTimeoutScale
{
    /// <summary>Selectable timeouts in minutes, ascending.</summary>
    public static IReadOnlyList<int> Steps { get; } = BuildSteps();

    public static int MaxIndex => Steps.Count - 1;

    /// <summary>Minutes for a slider position, clamped to the scale.</summary>
    public static int MinutesAt(int index) => Steps[Math.Clamp(index, 0, MaxIndex)];

    /// <summary>The slider position closest to <paramref name="minutes"/>; ties go to the shorter timeout.</summary>
    public static int IndexOf(int minutes)
    {
        var best = 0;
        for (var i = 1; i < Steps.Count; i++)
        {
            if (Math.Abs(Steps[i] - minutes) < Math.Abs(Steps[best] - minutes))
            {
                best = i;
            }
        }

        return best;
    }

    /// <summary>Formats minutes for display, e.g. "1 minute", "45 minutes", "1 hr 30 min", "24 hours".</summary>
    public static string Format(int minutes)
    {
        if (minutes < 60)
        {
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        var hours = minutes / 60;
        var remainder = minutes % 60;
        if (remainder == 0)
        {
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        return $"{hours} hr {remainder} min";
    }

    private static int[] BuildSteps()
    {
        var steps = new List<int>();
        AddRange(steps, 1, 5, 1);       // 1-5 minutes
        AddRange(steps, 10, 60, 5);     // 10-60 minutes
        AddRange(steps, 75, 180, 15);   // 1 hr 15 min - 3 hours
        AddRange(steps, 210, 480, 30);  // 3 hr 30 min - 8 hours
        AddRange(steps, 540, Settings.MaxIdleMinutes, 60); // 9-24 hours
        return steps.ToArray();
    }

    private static void AddRange(List<int> steps, int from, int to, int step)
    {
        for (var value = from; value <= to; value += step)
        {
            steps.Add(value);
        }
    }
}
