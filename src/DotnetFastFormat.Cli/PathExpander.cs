namespace DotnetFastFormat.Cli;

/// <summary>Turns the command-line paths into the files to format.</summary>
internal static class PathExpander
{
    /// <summary>Expands files and directories into files.</summary>
    /// <param name="paths">File and directory paths.</param>
    /// <param name="errors">Receives one message for each path that does not exist.</param>
    /// <returns>Each named file, then every <c>.cs</c> file under each named directory, in a stable order and without duplicates.</returns>
    public static List<string> Expand(IEnumerable<string> paths, List<string> errors)
    {
        var files = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in paths)
        {
            IEnumerable<string> found;
            if (Directory.Exists(path))
            {
                found = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal);
            }
            else if (File.Exists(path))
            {
                found = [path];
            }
            else
            {
                errors.Add($"{path}: no such file or directory");
                continue;
            }

            foreach (string file in found)
            {
                if (seen.Add(Path.GetFullPath(file)))
                {
                    files.Add(file);
                }
            }
        }

        return files;
    }
}
