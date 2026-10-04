namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// A document laid out flat when it fits in the remaining width and broken otherwise. Only the lines directly
/// inside the group follow its mode; groups nested inside it decide for themselves.
/// </summary>
internal sealed class GroupDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="GroupDoc"/> class.</summary>
    /// <param name="contents">The grouped document.</param>
    public GroupDoc(Doc contents)
    {
        Contents = contents;
        ForcesBreak = contents.ForcesBreak;
    }

    /// <summary>Gets the grouped document.</summary>
    public Doc Contents { get; }
}
