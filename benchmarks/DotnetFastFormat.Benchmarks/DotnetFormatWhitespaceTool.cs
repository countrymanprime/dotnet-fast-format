namespace DotnetFastFormat.Benchmarks;

/// <summary><c>dotnet format whitespace --folder</c>: the whitespace pass, which does not load MSBuild projects.</summary>
internal sealed class DotnetFormatWhitespaceTool : IBenchmarkTool
{
    public string Name => ToolNames.DotnetFormatWhitespace;

    public string Version() => ProcessRunner.Capture("dotnet", Environment.CurrentDirectory, "--version");

    public ProcessSpec? Prepare(string workingCopy) => null;

    public ProcessSpec? Command(string workingCopy) =>
        new("dotnet", ["format", "whitespace", ".", "--folder", "--verbosity", "minimal"], workingCopy);
}
