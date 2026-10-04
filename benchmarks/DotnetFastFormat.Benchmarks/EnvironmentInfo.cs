namespace DotnetFastFormat.Benchmarks;

/// <summary>Where and with what the numbers were measured.</summary>
/// <param name="OperatingSystem">OS description.</param>
/// <param name="Architecture">CPU architecture.</param>
/// <param name="Processors">Logical processor count.</param>
/// <param name="DotnetVersion">The .NET SDK version.</param>
/// <param name="RepositoryCommit">Commit of this repository the harness ran from.</param>
/// <param name="ToolVersions">Version of each measured tool, by tool name.</param>
internal sealed record EnvironmentInfo(
    string OperatingSystem,
    string Architecture,
    int Processors,
    string DotnetVersion,
    string RepositoryCommit,
    IReadOnlyDictionary<string, string> ToolVersions);
