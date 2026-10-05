namespace DotnetFastFormat.Benchmarks;

/// <summary>The formatter in this repository, run as the command-line tool users run.</summary>
internal sealed class FastFormatTool : IBenchmarkTool
{
    private static readonly string Executable = Path.Combine(AppContext.BaseDirectory, "dotnet-fast-format.dll");

    public string Name => ToolNames.FastFormat;

    /// <summary>
    /// The tool exits 2 after formatting everything it could when some input files do not parse, which happens
    /// in repositories that keep deliberately invalid code as test data. That run still did the work.
    /// </summary>
    /// <param name="outcome">The finished run.</param>
    /// <returns>True when the run formatted the repository.</returns>
    public static bool IsSuccessfulRun(ProcessOutcome outcome) =>
        outcome.ExitCode == 0
        || (outcome.ExitCode == 2 && outcome.OutputTail.Contains("Formatted ", StringComparison.Ordinal));

    public string Version() => ProcessRunner.Capture("dotnet", Environment.CurrentDirectory, Executable, "--version");

    public ProcessSpec? Prepare(string workingCopy) => null;

    public ProcessSpec? Command(string workingCopy) => new("dotnet", [Executable, "."], workingCopy);

    bool IBenchmarkTool.IsSuccess(ProcessOutcome outcome) => IsSuccessfulRun(outcome);
}
