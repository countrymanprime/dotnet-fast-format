using DotnetFastFormat.Benchmarks;

namespace DotnetFastFormat.Tests.Benchmarks;

public class BenchmarkOptionsTests
{
    [Fact]
    public void DefaultsRunEverythingThreeTimes()
    {
        (BenchmarkOptions? options, string? error) = BenchmarkOptions.Parse([]);

        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal(3, options.Runs);
        Assert.Empty(options.Repositories);
        Assert.Empty(options.Tools);
        Assert.Null(options.OutputDirectory);
    }

    [Fact]
    public void ParsesEveryOption()
    {
        (BenchmarkOptions? options, string? error) = BenchmarkOptions.Parse(
            ["--runs", "5", "--repo", "dapper", "--repo", "serilog", "--tool", "csharpier", "--output", "out"]);

        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal(5, options.Runs);
        Assert.True(options.Repositories.SetEquals(["dapper", "serilog"]));
        Assert.True(options.Tools.SetEquals(["csharpier"]));
        Assert.Equal("out", options.OutputDirectory);
    }

    [Theory]
    [InlineData("--runs", "0")]
    [InlineData("--runs", "abc")]
    [InlineData("--runs", "-1")]
    [InlineData("--tool", "nope")]
    [InlineData("--bogus", "1")]
    [InlineData("--runs", "")]
    public void RejectsBadArguments(string name, string value)
    {
        (BenchmarkOptions? options, string? error) = BenchmarkOptions.Parse([name, value]);

        Assert.Null(options);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void RejectsAnOptionWithoutAValue()
    {
        (BenchmarkOptions? options, string? error) = BenchmarkOptions.Parse(["--runs"]);

        Assert.Null(options);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
