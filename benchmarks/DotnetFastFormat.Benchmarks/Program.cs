using DotnetFastFormat.Benchmarks;

(BenchmarkOptions? options, string? error) = BenchmarkOptions.Parse(args);
if (options is null)
{
    Console.Error.WriteLine(error);
    Console.Error.WriteLine(BenchmarkOptions.Usage);
    return 2;
}

try
{
    if (options.SelfCheckCost)
    {
        string table = SelfCheckCost.Run(options, Console.Error);
        Console.Out.Write(table);
        if (options.OutputDirectory is { } costDirectory)
        {
            Directory.CreateDirectory(costDirectory);
            File.WriteAllText(Path.Combine(costDirectory, "self-check.md"), table);
        }

        return 0;
    }

    BenchmarkReport report = BenchmarkRunner.Run(options, Console.Error);
    string markdown = ReportWriter.ToMarkdown(report);
    Console.Out.Write(markdown);
    if (options.OutputDirectory is { } directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "baseline.md"), markdown);
        File.WriteAllText(Path.Combine(directory, "baseline.json"), ReportWriter.ToJson(report));
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 2;
}
