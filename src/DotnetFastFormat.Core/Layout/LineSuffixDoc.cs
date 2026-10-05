namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// Text written just before the next line break, which takes no width when deciding whether a group fits. Used
/// for a comment that trails a declaration, so a long comment never makes the declaration wrap (ADR 0010).
/// </summary>
internal sealed class LineSuffixDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="LineSuffixDoc"/> class.</summary>
    /// <param name="text">The text, which must not contain a line break.</param>
    public LineSuffixDoc(string text)
    {
        if (text.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            throw new ArgumentException("Suffix text must not contain a line break.", nameof(text));
        }

        Text = text;
    }

    /// <summary>Gets the text.</summary>
    public string Text { get; }
}
