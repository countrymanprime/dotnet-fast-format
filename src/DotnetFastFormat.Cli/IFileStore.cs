namespace DotnetFastFormat.Cli;

/// <summary>Reads and writes the files being formatted, so tests can observe and fail writes.</summary>
internal interface IFileStore
{
    /// <summary>Reads all bytes of <paramref name="path"/>.</summary>
    /// <param name="path">The file.</param>
    /// <returns>The file's contents.</returns>
    byte[] Read(string path);

    /// <summary>Replaces <paramref name="path"/> so that a reader sees either the old or the new contents, never a mix.</summary>
    /// <param name="path">The file to replace.</param>
    /// <param name="contents">The new contents.</param>
    void ReplaceAtomically(string path, byte[] contents);
}
