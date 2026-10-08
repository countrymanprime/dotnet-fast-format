namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// Items separated by lines, packed onto as few lines as fit: the parts alternate item, separator, item,
/// separator, and a separator breaks only when the next item no longer fits on the current line.
/// </summary>
internal sealed class FillDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="FillDoc"/> class.</summary>
    /// <param name="parts">Alternating items and separators, starting and ending with an item.</param>
    public FillDoc(IReadOnlyList<Doc> parts)
    {
        Parts = parts;
        for (int i = 0; i < parts.Count; i++)
        {
            ForcesBreak |= parts[i].ForcesBreak;
            WillBreak |= parts[i].WillBreak;
        }
    }

    /// <summary>Gets the alternating items and separators.</summary>
    public IReadOnlyList<Doc> Parts { get; }
}
