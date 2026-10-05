namespace DotnetFastFormat.Core;

/// <summary>What to do with a UTF-8 byte order mark when a file is written. The formatter works on text, so the caller that reads and writes bytes applies it.</summary>
public enum ByteOrderMarkPolicy
{
    /// <summary>Write the byte order mark the file had, if any.</summary>
    Preserve,

    /// <summary>Write no byte order mark (<c>charset = utf-8</c>).</summary>
    Remove,

    /// <summary>Write a byte order mark (<c>charset = utf-8-bom</c>).</summary>
    Add,
}
