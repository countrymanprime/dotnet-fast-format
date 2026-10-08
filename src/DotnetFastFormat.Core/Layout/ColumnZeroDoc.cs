namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// A document printed from column 0, whatever the indentation around it: the whitespace already written on the
/// current line is removed and lines inside it are not indented. The line break after it returns to the
/// surrounding indentation. Used for preprocessor directives and disabled text (ADR 0010).
/// </summary>
internal sealed class ColumnZeroDoc : Doc
{
    /// <summary>Initializes a new instance of the <see cref="ColumnZeroDoc"/> class.</summary>
    /// <param name="contents">The document to print from column 0.</param>
    public ColumnZeroDoc(Doc contents)
    {
        Contents = contents;
        ForcesBreak = contents.ForcesBreak;
    }

    /// <summary>Gets the document printed from column 0.</summary>
    public Doc Contents { get; }
}
