namespace DotnetFastFormat.Benchmarks;

/// <summary>Finds the solution file <c>dotnet format</c> should load for a repository.</summary>
internal static class SolutionFinder
{
    /// <summary>
    /// Returns the shallowest <c>.slnx</c> or <c>.sln</c> file under <paramref name="root"/>, preferring
    /// <c>.slnx</c> at equal depth, or null when there is none. Files such as <c>.sln.old</c> are ignored.
    /// </summary>
    public static string? Find(string root) =>
        Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            .Where(path => !Path.GetRelativePath(root, path).StartsWith(".git", StringComparison.Ordinal))
            .OrderBy(path => Path.GetRelativePath(root, path).Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .ThenBy(path => path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();
}
