using System.Diagnostics;
using System.Globalization;
using System.Text;
using DotnetFastFormat.Core;
using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Benchmarks;

/// <summary>
/// Times, in one process and without file writes, how long the formatter takes over a repository and how long
/// the always-on output check (ADR 0009) takes over the same files, so the cost of the check is known.
/// </summary>
internal static class SelfCheckCost
{
    /// <summary>Measures each selected repository and renders a Markdown table.</summary>
    /// <param name="options">The runs, repositories and output directory.</param>
    /// <param name="progress">Where progress lines go.</param>
    /// <returns>The Markdown report.</returns>
    public static string Run(BenchmarkOptions options, TextWriter progress)
    {
        var text = new StringBuilder();
        text.AppendLine("# Cost of the output check");
        text.AppendLine();
        text.AppendLine($"In-process, no file writes; median of {options.Runs} runs after one warm-up. "
            + "\"Format\" is `RoslynFormatter.Format` over every parseable file; \"Check\" is `OutputVerifier.Verify` over the same input and output.");
        text.AppendLine();
        text.AppendLine("| Repository | Files formatted | Format (s) | Check (s) | Check as share of format + check |");
        text.AppendLine("|---|---:|---:|---:|---:|");

        foreach (CorpusRepository repository in CorpusManifest.Load(CorpusManifest.DefaultPath())
            .Where(r => options.Repositories.Count == 0 || options.Repositories.Contains(r.Name)))
        {
            progress.WriteLine($"{repository.Name}: fetching pinned commit {repository.Sha[..10]}");
            string checkout = CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot()).Path;
            string[] sources = [.. Directory.EnumerateFiles(checkout, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(File.ReadAllText)];

            (string Source, string Output)[] pairs = Format(sources);
            _ = Time(pairs, static p => OutputVerifier.Verify(p.Source, p.Output));

            var formatTimes = new List<double>();
            var checkTimes = new List<double>();
            for (int run = 0; run < options.Runs; run++)
            {
                formatTimes.Add(Time(sources, static s => Try(s)));
                checkTimes.Add(Time(pairs, static p => OutputVerifier.Verify(p.Source, p.Output)));
            }

            double format = Stats.Summarize(formatTimes)!.Median;
            double check = Stats.Summarize(checkTimes)!.Median;
            text.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {repository.Name} | {pairs.Length} | {format:0.00} | {check:0.00} | {check / (format + check):P0} |"));
        }

        return text.ToString();
    }

    private static (string Source, string Output)[] Format(string[] sources)
    {
        var pairs = new List<(string, string)>();
        foreach (string source in sources)
        {
            if (Try(source) is { } output)
            {
                pairs.Add((source, output));
            }
        }

        return [.. pairs];
    }

    private static string? Try(string source)
    {
        try
        {
            return new RoslynFormatter().Format(source);
        }
        catch (FormatterException)
        {
            return null;
        }
    }

    private static double Time<T>(IReadOnlyList<T> items, Func<T, object?> work)
    {
        var watch = Stopwatch.StartNew();
        foreach (T item in items)
        {
            _ = work(item);
        }

        return watch.Elapsed.TotalSeconds;
    }
}
