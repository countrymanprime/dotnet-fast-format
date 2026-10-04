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

    public string Version() => ProcessRunner.Capture(executable, Environment.CurrentDirectory, "--version");

    public ProcessSpec? Prepare(string workingCopy) => null;

    public ProcessSpec? Command(string workingCopy) => new(executable, ["format", "."], workingCopy);
}
