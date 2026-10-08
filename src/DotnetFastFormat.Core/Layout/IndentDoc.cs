namespace DotnetFastFormat.Core.Layout;

/// <summary>A document whose line breaks are indented one level deeper.</summary>
internal sealed class IndentDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="IndentDoc"/> class.</summary>
    /// <param name="contents">The indented document.</param>
    public IndentDoc(Doc contents)
    {
        Contents = contents;
        ForcesBreak = contents.ForcesBreak;
        WillBreak = contents.WillBreak;
    }

    /// <summary>Gets the indented document.</summary>
    public Doc Contents { get; }
}
