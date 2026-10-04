namespace DotnetFastFormat.Benchmarks;

/// <summary><c>dotnet format</c> on the repository's solution. This is the MSBuild-based tool this project aims to replace.</summary>
internal sealed class DotnetFormatTool : IBenchmarkTool
{
    public string Name => ToolNames.DotnetFormat;

    public string Version() => ProcessRunner.Capture("dotnet", Environment.CurrentDirectory, "--version");

    // Restoring first keeps NuGet traffic out of the timing: a repository being formatted is normally already restored.
    public ProcessSpec? Prepare(string workingCopy) =>
        SolutionFinder.Find(workingCopy) is { } solution
            ? new ProcessSpec("dotnet", ["restore", solution, "--verbosity", "quiet"], workingCopy)
            : null;

    public ProcessSpec? Command(string workingCopy) =>
        SolutionFinder.Find(workingCopy) is { } solution
            ? new ProcessSpec("dotnet", ["format", solution, "--no-restore", "--verbosity", "minimal"], workingCopy)
            : null;
}
