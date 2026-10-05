public static class TimingHelper
{
    private static readonly double TickToSeconds = 1.0 / System.Diagnostics.Stopwatch.Frequency;

    public static double GetTimeSeconds()
    {
        return System.Diagnostics.Stopwatch.GetTimestamp() * TickToSeconds;
    }
}
