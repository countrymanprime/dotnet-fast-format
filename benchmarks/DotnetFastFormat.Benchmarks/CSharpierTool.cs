namespace DotnetFastFormat.Benchmarks;

/// <summary>CSharpier, installed at a pinned version into a directory the harness owns.</summary>
internal sealed class CSharpierTool : IBenchmarkTool
{
    /// <summary>The CSharpier version that is measured. Change it deliberately: it changes the baseline.</summary>
    public const string PinnedVersion = "1.3.0";

    private readonly string executable;

    public CSharpierTool(string toolsDirectory)
    {
        string directory = Path.Combine(toolsDirectory, "csharpier-" + PinnedVersion);
        executable = Path.Combine(directory, OperatingSystem.IsWindows() ? "csharpier.exe" : "csharpier");
        if (!File.Exists(executable))
        {
            Directory.CreateDirectory(directory);
            ProcessRunner.Capture("dotnet", directory, "tool", "install", "csharpier", "--version", PinnedVersion, "--tool-path", directory);
        }
    }

    public string Name => ToolNames.CSharpier;

    /// <summary>
    /// CSharpier exits with 1 after formatting everything it could when some input files do not parse, which
    /// happens in repositories that keep deliberately invalid code as test data. That run still did the work,
    /// so exit code 1 counts when CSharpier reported that it formatted files.
    /// </summary>
    /// <param name="outcome">The finished run.</param>
    /// <returns>True when the run formatted the repository.</returns>
    public static bool IsSuccessfulRun(ProcessOutcome outcome) =>
        outcome.ExitCode == 0
        || (outcome.ExitCode == 1 && outcome.OutputTail.Contains("Formatted ", StringComparison.Ordinal));

    public string Version() => ProcessRunner.Capture(executable, Environment.CurrentDirectory, "--version");

    public ProcessSpec? Prepare(string workingCopy) => null;

    public ProcessSpec? Command(string workingCopy) => new(executable, ["format", "."], workingCopy);

    bool IBenchmarkTool.IsSuccess(ProcessOutcome outcome) => IsSuccessfulRun(outcome);
}
