using System.Text;

namespace DotnetFastFormat.Cli;

/// <summary>Reads <c>.editorconfig</c> files from disk for the resolver.</summary>
internal static class DiskConfigReader
{
    /// <summary>Reads a configuration file.</summary>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>The text, or <see langword="null"/> when there is no such file (a directory with that name is not a file). Invalid UTF-8 is replaced, not rejected.</returns>
    /// <exception cref="IOException">The file exists but cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file exists but access is denied.</exception>
    public static string? Read(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(File.ReadAllBytes(path));
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }
}
