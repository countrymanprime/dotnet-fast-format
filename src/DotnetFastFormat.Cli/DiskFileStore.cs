namespace DotnetFastFormat.Cli;

/// <summary>The file system: writes go to a temporary file beside the target and are moved into place.</summary>
internal sealed class DiskFileStore : IFileStore
{
    /// <inheritdoc/>
    public byte[] Read(string path) => File.ReadAllBytes(path);

    /// <inheritdoc/>
    public void ReplaceAtomically(string path, byte[] contents)
    {
        string full = Path.GetFullPath(path);
        string temporary = Path.Combine(Path.GetDirectoryName(full)!, $".{Path.GetFileName(full)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporary, contents);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(temporary, File.GetUnixFileMode(full));
            }

            File.Move(temporary, full, overwrite: true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }
}
