namespace DotnetFastFormat.Corpus;

/// <summary>Deletes directory trees that git has written to.</summary>
public static class DirectoryCleanup
{
    /// <summary>Deletes a directory tree, clearing the read-only flag git sets on object files (Windows refuses otherwise).</summary>
    public static void Delete(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(path, recursive: true);
    }
}
