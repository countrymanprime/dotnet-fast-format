namespace DotnetFastFormat.Core.Layout;

/// <summary>How a document is laid out.</summary>
internal sealed record DocPrintOptions
{
    /// <summary>Initializes a new instance of the <see cref="DocPrintOptions"/> class.</summary>
    /// <param name="width">The line width in UTF-16 code units; a line of exactly this many fits.</param>
    /// <param name="indentSize">Spaces per indent level.</param>
    /// <param name="newLine">The line terminator the printer writes.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is below 1 or <paramref name="indentSize"/> is negative.</exception>
    public DocPrintOptions(int width = 100, int indentSize = 4, string newLine = "\n")
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(indentSize);
        Width = width;
        IndentSize = indentSize;
        NewLine = newLine;
    }

    /// <summary>Gets the line width in UTF-16 code units.</summary>
    public int Width { get; }

    /// <summary>Gets the spaces per indent level.</summary>
    public int IndentSize { get; }

    /// <summary>Gets the line terminator the printer writes.</summary>
    public string NewLine { get; }
}
