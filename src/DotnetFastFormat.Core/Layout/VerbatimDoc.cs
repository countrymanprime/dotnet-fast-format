namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// Source text emitted exactly as written, including any line breaks it contains (see ADR 0008). It is never
/// re-indented, and trailing whitespace inside it is never trimmed. Multi-line verbatim text forces its
/// enclosing groups to break, unless it is a literal (a string), which only counts up to its first line break.
/// </summary>
internal sealed class VerbatimDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="VerbatimDoc"/> class.</summary>
    /// <param name="value">The text to emit.</param>
    /// <param name="forcesBreak">Whether text with a line break forces its enclosing groups to break. A string literal does not: the layout around it is decided by the text up to its first line break.</param>
    public VerbatimDoc(string value, bool forcesBreak = true)
    {
        Value = value;
        ForcesBreak = forcesBreak && value.AsSpan().IndexOfAny('\r', '\n') >= 0;
        WillBreak = ForcesBreak;
    }

    /// <summary>Gets the text.</summary>
    public string Value { get; }
}
