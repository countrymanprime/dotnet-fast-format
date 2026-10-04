namespace DotnetFastFormat.Benchmarks;

/// <summary>Counts the files a formatter changed, from <c>git status --porcelain</c> output.</summary>
internal static class ChangedFiles
{
    /// <summary>
    /// Counts the modified tracked files in <paramref name="porcelainStatus"/>, ignoring <c>global.json</c>:
    /// the harness removes it from working copies so the installed SDK is used, and that is not formatting work.
    /// </summary>
    public static int Count(string porcelainStatus) =>
        porcelainStatus
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => !line.TrimEnd().EndsWith("global.json", StringComparison.Ordinal));
}
