namespace DotnetFastFormat.Core.Layout;

/// <summary>Text on a single line. Its width is its length in UTF-16 code units.</summary>
internal sealed class TextDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="TextDoc"/> class.</summary>
    /// <param name="value">The text, which must not contain a line break.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> contains a line break.</exception>
    public TextDoc(string value)
    {
        if (value.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            throw new ArgumentException("Text must not contain a line break; use a line or verbatim text.", nameof(value));
        }

        Value = value;
    }

    /// <summary>Gets the text.</summary>
    public string Value { get; }
}
