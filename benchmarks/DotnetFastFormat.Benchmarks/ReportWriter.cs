using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DotnetFastFormat.Benchmarks;

/// <summary>Renders a <see cref="BenchmarkReport"/> as Markdown or JSON.</summary>
internal static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Serializes the report.</summary>
    public static string ToJson(BenchmarkReport report) => JsonSerializer.Serialize(report, JsonOptions);

    /// <summary>Reads a report written by <see cref="ToJson"/>.</summary>
    public static BenchmarkReport? FromJson(string json) => JsonSerializer.Deserialize<BenchmarkReport>(json, JsonOptions);

    /// <summary>Renders the report as a Markdown document with the method, the environment and a results table.</summary>
    public static string ToMarkdown(BenchmarkReport report)
    {
        string[] tools = [.. report.Results
            .Select(r => r.Tool)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tool => IndexOf(tool))];

        var text = new StringBuilder();
        text.AppendLine("# Formatting speed baseline");
        text.AppendLine();
        text.AppendLine("Wall-clock time to format each corpus repository in place. Each cell is the median of "
            + $"{report.Runs} runs (min–max); every run formats a fresh copy of the pinned checkout, and the time "
            + "excludes copying and, for `dotnet-format`, the restore. \"Files changed\" counts tracked files the tool "
            + "modified, because the tools do different amounts of work.");
        text.AppendLine();
        text.AppendLine(FormattableString.Invariant($"- Operating system: {report.Environment.OperatingSystem} ({report.Environment.Architecture}, {report.Environment.Processors} logical processors)"));
        text.AppendLine(FormattableString.Invariant($"- .NET SDK: {report.Environment.DotnetVersion}"));
        string toolVersions = string.Join(", ", report.Environment.ToolVersions.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value}"));
        text.AppendLine(FormattableString.Invariant($"- Tools: {toolVersions}"));
        text.AppendLine(FormattableString.Invariant($"- Harness commit: {report.Environment.RepositoryCommit}"));
        text.AppendLine();

        text.AppendLine("| Repository | .cs files | MB | " + string.Join(" | ", tools) + " |");
        text.AppendLine("|---|---:|---:|" + string.Concat(tools.Select(_ => "---|")));
        foreach (RepositoryInfo repository in report.Repositories)
        {
            text.AppendLine(FormattableString.Invariant($"| {repository.Name} | {repository.CsFiles} | {Format(repository.Megabytes)} | ")
                + string.Join(" | ", tools.Select(tool => Cell(report, repository.Name, tool))) + " |");
        }

        text.AppendLine(FormattableString.Invariant($"| **Total** | {report.Repositories.Sum(r => r.CsFiles)} | {Format(report.Repositories.Sum(r => r.Megabytes))} | ")
            + string.Join(" | ", tools.Select(tool => Total(report, tool))) + " |");
        return text.ToString();
    }

    private static int IndexOf(string tool)
    {
        for (int i = 0; i < ToolNames.All.Count; i++)
        {
            if (string.Equals(ToolNames.All[i], tool, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return ToolNames.All.Count;
    }

    private static string Cell(BenchmarkReport report, string repository, string tool)
    {
        ToolResult? result = report.Results.FirstOrDefault(r =>
            string.Equals(r.Repository, repository, StringComparison.Ordinal) && string.Equals(r.Tool, tool, StringComparison.Ordinal));
        if (result is null)
        {
            return "n/a";
        }

        Measurement[] succeeded = [.. result.Runs.Where(run => run.Succeeded)];
        Summary? summary = Stats.Summarize(succeeded.Select(run => run.Seconds));
        if (summary is null)
        {
            return FormattableString.Invariant($"failed ({(result.Runs.Count > 0 ? result.Runs[0].Error : null) ?? "no runs"})");
        }

        string cell = $"{Format(summary.Median)} s ({Format(summary.Min)}–{Format(summary.Max)}), {succeeded[0].ChangedFiles} files changed";
        return succeeded.Length < result.Runs.Count ? $"{cell}; {result.Runs.Count - succeeded.Length} of {result.Runs.Count} runs failed" : cell;
    }

    private static string Total(BenchmarkReport report, string tool)
    {
        double total = 0;
        foreach (RepositoryInfo repository in report.Repositories)
        {
            ToolResult? result = report.Results.FirstOrDefault(r =>
                string.Equals(r.Repository, repository.Name, StringComparison.Ordinal) && string.Equals(r.Tool, tool, StringComparison.Ordinal));
            Summary? summary = result is null ? null : Stats.Summarize(result.Runs.Where(run => run.Succeeded).Select(run => run.Seconds));
            if (summary is null)
            {
                return "n/a";
            }

            total += summary.Median;
        }

        return $"{Format(total)} s";
    }

    private static string Format(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
}
