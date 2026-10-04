namespace DotnetFastFormat.Benchmarks;

/// <summary>Summary statistics for timings.</summary>
internal static class Stats
{
    /// <summary>Summarizes <paramref name="values"/>, or returns null when there are none.</summary>
    public static Summary? Summarize(IEnumerable<double> values)
    {
        double[] sorted = [.. values.Order()];
        if (sorted.Length == 0)
        {
            return null;
        }

        int middle = sorted.Length / 2;
        double median = sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
        return new Summary(median, sorted[0], sorted[^1]);
    }
}
