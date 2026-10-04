using DotnetFastFormat.Benchmarks;

namespace DotnetFastFormat.Tests.Benchmarks;

public class ReportWriterTests
{
    private static readonly BenchmarkReport Report = new(
        new EnvironmentInfo("Linux 6", "X64", 4, ".NET 10", "abc123", new Dictionary<string, string>(StringComparer.Ordinal) { ["csharpier"] = "1.3.0" }),
        3,
        [new RepositoryInfo("dapper", 157, 3.0), new RepositoryInfo("serilog", 216, 3.4)],
        [
            new ToolResult("dapper", "csharpier", [new Measurement(true, 2.0, 140, null), new Measurement(true, 3.0, 140, null), new Measurement(true, 2.5, 140, null)]),
            new ToolResult("dapper", "dotnet-format", [new Measurement(false, 1.0, 0, "exit code 1")]),
            new ToolResult("serilog", "csharpier", [new Measurement(true, 1.0, 100, null), new Measurement(true, 1.2, 100, null), new Measurement(true, 1.1, 100, null)]),
        ]);

    [Fact]
    public void WritesTheEnvironmentAndTheMethod()
    {
        string markdown = ReportWriter.ToMarkdown(Report);

        Assert.Contains("Linux 6", markdown, StringComparison.Ordinal);
        Assert.Contains("abc123", markdown, StringComparison.Ordinal);
        Assert.Contains("csharpier 1.3.0", markdown, StringComparison.Ordinal);
        Assert.Contains("3 runs", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void WritesMedianRangeAndChangedFiles()
    {
        string markdown = ReportWriter.ToMarkdown(Report);

        Assert.Contains("2.5 s (2.0–3.0), 140 files changed", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void MarksFailedAndMissingMeasurements()
    {
        string markdown = ReportWriter.ToMarkdown(Report);

        Assert.Contains("failed (exit code 1)", markdown, StringComparison.Ordinal);
        Assert.Contains("n/a", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void TruncatesLongErrorsInTheTable()
    {
        string longError = new('x', 500);
        var report = Report with
        {
            Results = [new ToolResult("dapper", "csharpier", [new Measurement(false, 1.0, 0, longError)])],
        };

        string markdown = ReportWriter.ToMarkdown(report);

        Assert.DoesNotContain(longError, markdown, StringComparison.Ordinal);
        Assert.Contains("…)", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void TotalsOnlyCoverRepositoriesWhereTheToolSucceeded()
    {
        string markdown = ReportWriter.ToMarkdown(Report);

        Assert.Contains("3.6 s", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonRoundTripsTheReport()
    {
        string json = ReportWriter.ToJson(Report);
        BenchmarkReport? parsed = ReportWriter.FromJson(json);

        Assert.Equal(Report.Runs, parsed?.Runs);
        Assert.Equal(Report.Results.Count, parsed?.Results.Count);
        Assert.Equal("abc123", parsed?.Environment.RepositoryCommit);
    }
}
