namespace DotnetFastFormat.Core.Layout;

/// <summary>Documents printed one after another.</summary>
internal sealed class ConcatDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="ConcatDoc"/> class.</summary>
    /// <param name="parts">The documents, in order.</param>
    public ConcatDoc(IReadOnlyList<Doc> parts)
    {
        Parts = parts;
        for (int i = 0; i < parts.Count; i++)
        {
            ForcesBreak |= parts[i].ForcesBreak;
            WillBreak |= parts[i].WillBreak;
        }
    }

    /// <summary>Gets the documents, in order.</summary>
    public IReadOnlyList<Doc> Parts { get; }
}
