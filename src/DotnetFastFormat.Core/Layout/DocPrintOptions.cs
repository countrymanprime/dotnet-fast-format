namespace DotnetFastFormat.Core.Layout;

/// <summary>How a document is laid out.</summary>
internal sealed record DocPrintOptions
{
    /// <summary>Initializes a new instance of the <see cref="DocPrintOptions"/> class.</summary>
    /// <param name="width">The line width in UTF-16 code units; a line of exactly this many fits.</param>
    /// <param name="indentSize">Columns per indent level.</param>
    /// <param name="newLine">The line terminator the printer writes.</param>
    /// <param name="useTabs">Whether indentation is written with tab characters (and spaces for a remainder) instead of spaces.</param>
    /// <param name="tabWidth">The columns one tab character counts for. Only used when <paramref name="useTabs"/> is set.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="tabWidth"/> is below 1, or <paramref name="indentSize"/> is negative.</exception>
    public DocPrintOptions(int width = 100, int indentSize = 4, string newLine = "\n", bool useTabs = false, int tabWidth = 4)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(indentSize);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabWidth, 1);
        Width = width;
        IndentSize = indentSize;
        NewLine = newLine;
        UseTabs = useTabs;
        TabWidth = tabWidth;
    }

    /// <summary>Gets the line width in UTF-16 code units.</summary>
    public int Width { get; }

    /// <summary>Gets the columns per indent level.</summary>
    public int IndentSize { get; }

    /// <summary>Gets the line terminator the printer writes.</summary>
    public string NewLine { get; }

    /// <summary>Gets a value indicating whether indentation is written with tabs.</summary>
    public bool UseTabs { get; }

    /// <summary>Gets the columns one tab character counts for.</summary>
    public int TabWidth { get; }
}
