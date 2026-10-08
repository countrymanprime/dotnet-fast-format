namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// A document laid out flat when it fits in the remaining width and broken otherwise. Only the lines directly
/// inside the group follow its mode; groups nested inside it decide for themselves.
/// </summary>
internal sealed class GroupDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="GroupDoc"/> class.</summary>
    /// <param name="contents">The grouped document.</param>
    /// <param name="forceBreak">Whether the group is always broken, whatever its width.</param>
    public GroupDoc(Doc contents, bool forceBreak = false)
    {
        Contents = contents;
        ForcesBreak = forceBreak || contents.ForcesBreak;
        WillBreak = forceBreak || contents.WillBreak;
    }

    /// <summary>Gets the grouped document.</summary>
    public Doc Contents { get; }
}
