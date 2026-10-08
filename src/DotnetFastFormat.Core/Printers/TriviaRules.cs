using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Core.Printers;

/// <summary>What the printers may do with trivia: only whitespace is rewritten (ADR 0008).</summary>
internal static class TriviaRules
{
    /// <summary>Returns whether <paramref name="trivia"/> is a comment, a documentation comment, a directive or disabled text.</summary>
    /// <param name="trivia">The trivia to classify.</param>
    /// <returns><see langword="true"/> for anything other than whitespace and line ends.</returns>
    public static bool IsCommentOrDirective(SyntaxTrivia trivia) =>
        !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia);

    /// <summary>Returns whether <paramref name="trivia"/> holds a comment, a documentation comment, a directive or disabled text. It allocates nothing.</summary>
    /// <param name="trivia">The trivia list to inspect.</param>
    /// <returns><see langword="true"/> when something other than whitespace and line ends is in the list.</returns>
    public static bool HasCommentOrDirective(SyntaxTriviaList trivia)
    {
        foreach (SyntaxTrivia piece in trivia)
        {
            if (IsCommentOrDirective(piece))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Counts the blank lines before the first token of <paramref name="node"/>, up to its first comment or directive.</summary>
    /// <param name="node">The node whose leading trivia is read.</param>
    /// <returns>The number of blank lines the author left above the node.</returns>
    public static int BlankLinesBefore(SyntaxNode node)
    {
        int count = 0;
        foreach (SyntaxTrivia trivia in node.GetFirstToken().LeadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                count++;
            }
            else if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                break;
            }
        }

        return count;
    }
}
