namespace DotnetFastFormat.Benchmarks;

/// <summary>Where the harness keeps its working copies.</summary>
internal static class WorkspacePaths
{
    /// <summary>
    /// The directory working copies are created in. It must be outside the repository: MSBuild, <c>.editorconfig</c>
    /// and <c>global.json</c> resolution all search upward, so a working copy inside this repository would be
    /// built with this repository's <c>Directory.Build.props</c>, <c>Directory.Packages.props</c> and <c>.editorconfig</c>.
    /// </summary>
    /// <param name="overridePath">A directory to use instead of the system temporary directory, or null.</param>
    public static string WorkRoot(string? overridePath) =>
        string.IsNullOrWhiteSpace(overridePath)
            ? Path.Combine(Path.GetTempPath(), "dotnet-fast-format-bench", "work")
            : Path.GetFullPath(overridePath);
}
