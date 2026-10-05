namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// Source text emitted exactly as written, including any line breaks it contains (see ADR 0008). It is never
/// re-indented, and trailing whitespace inside it is never trimmed. Multi-line verbatim text forces its
/// enclosing groups to break.
/// </summary>
internal sealed class VerbatimDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="VerbatimDoc"/> class.</summary>
    /// <param name="value">The text to emit.</param>
    public VerbatimDoc(string value)
    {
        Value = value;
        ForcesBreak = value.AsSpan().IndexOfAny('\r', '\n') >= 0;
    }

    /// <summary>Gets the text.</summary>
    public string Value { get; }
}
