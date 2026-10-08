using DotnetFastFormat.Core.Layout;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Turns own-line trivia into documents (ADR 0010).</summary>
internal static class TriviaPrinter
{
    /// <summary>Prints one own-line piece, with no line break before or after it.</summary>
    /// <param name="line">The comment, directive or disabled text.</param>
    /// <returns>The document. A directive or disabled text starts at column 0; a comment is indented with the code.</returns>
    public static Doc Line(TriviaLine line) => line.Kind switch
    {
        TriviaLineKind.Directive => Docs.ColumnZero(Docs.Text(line.Text[0])),
        TriviaLineKind.DisabledText => Docs.ColumnZero(Docs.Verbatim(line.RawText!)),
        TriviaLineKind.Region => Docs.Text(line.Text[0]),
        _ => line.RawText is { } raw
            ? Docs.Verbatim(raw)
            : Docs.Join(Docs.HardLine, line.Text.Select(Docs.Text)),
    };

    /// <summary>Prints the leading lines of an item, each followed by a line break, so the item can follow directly.</summary>
    /// <param name="leading">The lines above the item, or <see langword="null"/>.</param>
    /// <returns>The document, empty when there are no lines.</returns>
    public static Doc Leading(LeadingTrivia? leading)
    {
        if (leading is null || leading.Lines.Count == 0)
        {
            return Docs.Empty;
        }

        var parts = new List<Doc>();
        for (int i = 0; i < leading.Lines.Count; i++)
        {
            if (i > 0)
            {
                parts.Add(Docs.HardLine);
                if (leading.Lines[i].BlankLineBefore)
                {
                    parts.Add(Docs.HardLine);
                }
            }

            parts.Add(Line(leading.Lines[i]));
        }

        parts.Add(Docs.HardLine);
        if (leading.BlankLineAfter)
        {
            parts.Add(Docs.HardLine);
        }

        return Docs.Concat(parts);
    }

    /// <summary>Prints the closing lines of a body or file: each on its own line, after the last item.</summary>
    /// <param name="closing">The lines before the closing brace or the end of the file, or <see langword="null"/>.</param>
    /// <param name="hasItemsAbove">Whether an item precedes them, so the first line needs a break (and may keep a blank line) before it.</param>
    /// <returns>The document, empty when there are no lines.</returns>
    public static Doc Closing(LeadingTrivia? closing, bool hasItemsAbove)
    {
        if (closing is null || closing.Lines.Count == 0)
        {
            return Docs.Empty;
        }

        var parts = new List<Doc>();
        for (int i = 0; i < closing.Lines.Count; i++)
        {
            if (i > 0 || hasItemsAbove)
            {
                parts.Add(Docs.HardLine);
                if (closing.Lines[i].BlankLineBefore && (i > 0 || hasItemsAbove))
                {
                    parts.Add(Docs.HardLine);
                }
            }

            parts.Add(Line(closing.Lines[i]));
        }

        return Docs.Concat(parts);
    }
}
