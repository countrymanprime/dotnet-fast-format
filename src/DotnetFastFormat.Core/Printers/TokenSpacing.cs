using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Where a space goes between two tokens of a declaration header (names, types, constraints, simple values).</summary>
internal static class TokenSpacing
{
    /// <summary>Returns whether a single space separates <paramref name="previous"/> and <paramref name="next"/>.</summary>
    /// <param name="previous">The token on the left.</param>
    /// <param name="next">The token on the right.</param>
    /// <returns><see langword="true"/> when a space is printed between them.</returns>
    public static bool NeedsSpace(SyntaxToken previous, SyntaxToken next)
    {
        SyntaxKind before = previous.Kind();
        SyntaxKind after = next.Kind();

        if (after is SyntaxKind.DotToken or SyntaxKind.ColonColonToken or SyntaxKind.SemicolonToken
            or SyntaxKind.CommaToken or SyntaxKind.GreaterThanToken or SyntaxKind.LessThanToken
            or SyntaxKind.OpenBracketToken or SyntaxKind.CloseBracketToken or SyntaxKind.QuestionToken
            or SyntaxKind.CloseParenToken or SyntaxKind.AsteriskToken)
        {
            return false;
        }

        if (before is SyntaxKind.DotToken or SyntaxKind.ColonColonToken or SyntaxKind.LessThanToken
            or SyntaxKind.OpenBracketToken or SyntaxKind.OpenParenToken)
        {
            return false;
        }

        if (after == SyntaxKind.OpenParenToken)
        {
            return before is not (SyntaxKind.IdentifierToken or SyntaxKind.GreaterThanToken or SyntaxKind.NewKeyword);
        }

        return !(before == SyntaxKind.CommaToken && after is SyntaxKind.CommaToken or SyntaxKind.CloseBracketToken);
    }
}
