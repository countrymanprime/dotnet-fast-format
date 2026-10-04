using DotnetFastFormat.Benchmarks;

namespace DotnetFastFormat.Tests.Benchmarks;

public class BenchmarkStatsTests
{
    [Fact]
    public void SummarizesAnOddCount()
    {
        Summary? summary = Stats.Summarize([3.0, 1.0, 2.0]);

        Assert.Equal(new Summary(2.0, 1.0, 3.0), summary);
    }

    [Fact]
    public void TheMedianOfAnEvenCountIsTheMeanOfTheMiddlePair()
    {
        Summary? summary = Stats.Summarize([4.0, 1.0, 2.0, 3.0]);

        Assert.Equal(2.5, summary?.Median);
    }

    [Fact]
    public void ASingleValueIsItsOwnSummary() =>
        Assert.Equal(new Summary(7.0, 7.0, 7.0), Stats.Summarize([7.0]));

    [Fact]
    public void NoValuesHaveNoSummary() =>
        Assert.Null(Stats.Summarize([]));
}
