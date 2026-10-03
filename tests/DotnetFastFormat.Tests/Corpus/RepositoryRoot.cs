namespace DotnetFastFormat.Tests.Corpus;

/// <summary>Locates the repository checkout the tests run from.</summary>
public static class RepositoryRoot
{
    private const string Marker = "DotnetFastFormat.slnx";

    /// <summary>Walks up from the test binaries to the directory holding the solution file.</summary>
    /// <returns>The absolute path of the repository root.</returns>
    public static string Find()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, Marker)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not find {Marker} above {AppContext.BaseDirectory}.");
    }
}
