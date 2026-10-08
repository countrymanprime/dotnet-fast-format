using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Reads the trivia around a token into the shapes the printers can place (ADR 0010).</summary>
internal static class TriviaLines
{
    private static readonly LeadingTrivia NoLines = new([], BlankLineAfter: false);
    private static readonly LeadingTrivia NoLinesBlankAfter = new([], BlankLineAfter: true);

    /// <summary>Reads the trivia before a token.</summary>
    /// <param name="trivia">The token's leading trivia, which starts at the beginning of a line.</param>
    /// <param name="atEndOfFile">Whether the token is the end of the file, where a last comment needs no line end.</param>
    /// <returns>The own-line pieces, or <see langword="null"/> when a comment shares its line with code or another comment.</returns>
    public static LeadingTrivia? ParseLeading(SyntaxTriviaList trivia, bool atEndOfFile)
    {
        List<TriviaLine>? lines = null;
        int blanks = 0;
        bool lineOpen = false;

        foreach (SyntaxTrivia piece in trivia)
        {
            SyntaxKind kind = piece.Kind();
            if (kind == SyntaxKind.WhitespaceTrivia)
            {
                continue;
            }

            if (kind == SyntaxKind.EndOfLineTrivia)
            {
                if (lineOpen)
                {
                    lineOpen = false;
                }
                else
                {
                    blanks++;
                }

                continue;
            }

            if (lineOpen || Classify(piece) is not { } entry)
            {
                return null;
            }

            (lines ??= []).Add(new TriviaLine(entry.Kind, entry.Text, blanks > 0, entry.RawText));
            blanks = 0;
            lineOpen = entry.LineStaysOpen;
        }

        if (lineOpen && !atEndOfFile)
        {
            return null;
        }

        // Most nodes have no comment above them, so the common answers are shared.
        return lines is null ? (blanks > 0 ? NoLinesBlankAfter : NoLines) : new LeadingTrivia(lines, blanks > 0);
    }

    /// <summary>Reads the trivia after a token, up to the end of its line.</summary>
    /// <param name="trivia">The token's trailing trivia.</param>
    /// <returns>Whether the shape is one a printer handles, and the comment if there is one. A comment is clean when it is the only one, does not span lines, and (for a block comment) ends its line.</returns>
    public static (bool Clean, string? Comment) ParseTrailing(SyntaxTriviaList trivia)
    {
        string? comment = null;
        bool endsLine = false;
        foreach (SyntaxTrivia piece in trivia)
        {
            SyntaxKind kind = piece.Kind();
            if (kind == SyntaxKind.EndOfLineTrivia)
            {
                endsLine = true;
                continue;
            }

            if (kind == SyntaxKind.WhitespaceTrivia)
            {
                continue;
            }

            string text = piece.ToFullString();
            bool singleLine = kind == SyntaxKind.SingleLineCommentTrivia
                || (kind == SyntaxKind.MultiLineCommentTrivia && !text.Contains('\n', StringComparison.Ordinal) && !text.Contains('\r', StringComparison.Ordinal));
            if (comment is not null || !singleLine)
            {
                return (false, null);
            }

            comment = text.TrimEnd();
        }

        // A block comment with more code after it on its line belongs to that code, not to the token before it.
        return comment is not null && !endsLine && !comment.StartsWith("//", StringComparison.Ordinal) ? (false, null) : (true, comment);
    }

    private static (TriviaLineKind Kind, string[] Text, bool LineStaysOpen, string? RawText)? Classify(SyntaxTrivia piece)
    {
        switch (piece.Kind())
        {
            case SyntaxKind.SingleLineCommentTrivia:
                return (TriviaLineKind.Comment, [piece.ToFullString().TrimEnd()], true, null);
            case SyntaxKind.MultiLineCommentTrivia:
            case SyntaxKind.MultiLineDocumentationCommentTrivia:
                return (TriviaLineKind.Comment, SplitLines(piece.ToFullString().TrimEnd(), trimIndent: false), true, piece.ToFullString().TrimEnd());
            case SyntaxKind.SingleLineDocumentationCommentTrivia:
                return (TriviaLineKind.Comment, SplitLines(piece.ToFullString().TrimEnd(), trimIndent: true), false, null);
            case SyntaxKind.RegionDirectiveTrivia:
            case SyntaxKind.EndRegionDirectiveTrivia:
                return (TriviaLineKind.Region, [piece.ToFullString().Trim()], false, null);
            case SyntaxKind.DisabledTextTrivia:
                return (TriviaLineKind.DisabledText, SplitLines(RemoveFinalLineEnd(piece.ToFullString()), trimIndent: false), false, RemoveFinalLineEnd(piece.ToFullString()));
            default:
                return piece.IsDirective ? (TriviaLineKind.Directive, [piece.ToFullString().Trim()], false, null) : null;
        }
    }

    private static string RemoveFinalLineEnd(string text) =>
        text.EndsWith("\r\n", StringComparison.Ordinal) ? text[..^2]
        : text.EndsWith('\n') ? text[..^1]
        : text;

    private static string[] SplitLines(string text, bool trimIndent)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            lines[i] = trimIndent ? line.TrimStart() : line;
        }

        return lines;
    }
}
