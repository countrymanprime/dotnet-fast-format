using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Core;

/// <summary>Checks that formatted output means the same as its input: valid, same tokens, no comment or directive lost.</summary>
public static class OutputVerifier
{
    /// <summary>Compares <paramref name="output"/> with <paramref name="input"/>.</summary>
    /// <param name="input">The original source text.</param>
    /// <param name="output">The formatted source text.</param>
    /// <returns>The first violated invariant, or success.</returns>
    public static VerificationResult Verify(string input, string output)
    {
        SyntaxTree before = SourceParser.Parse(input);
        SyntaxTree after = SourceParser.Parse(output);

        string? newError = FindNewError(before, after);
        if (newError is not null)
        {
            return Fail(OutputInvariant.ValidOutput, $"Output has new diagnostic {newError}.");
        }

        if (!Tokens(before).SequenceEqual(Tokens(after), StringComparer.Ordinal))
        {
            return Fail(OutputInvariant.TreePreserving, "Output tokens differ from input tokens.");
        }

        if (!CodeAndComments(before).SequenceEqual(CodeAndComments(after), StringComparer.Ordinal))
        {
            return Fail(OutputInvariant.NoLoss, "A comment or directive was dropped, changed or moved across code.");
        }

        return VerificationResult.Success;
    }

    private static VerificationResult Fail(OutputInvariant invariant, string message) => new(invariant, message);

    private static string? FindNewError(SyntaxTree input, SyntaxTree output)
    {
        Dictionary<string, int> before = CountErrors(input);
        foreach ((string id, int count) in CountErrors(output))
        {
            if (count > before.GetValueOrDefault(id))
            {
                return id;
            }
        }

        return null;
    }

    private static Dictionary<string, int> CountErrors(SyntaxTree tree) =>
        tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .GroupBy(d => d.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

    private static IEnumerable<string> Tokens(SyntaxTree tree) =>
        tree.GetRoot().DescendantTokens().Select(t => $"{t.Kind()}:{t.Text}");

    /// <summary>
    /// Code tokens and comment/directive trivia in source order, so a comment that moves across a token changes the
    /// sequence. Each comment also records where it sits: on its own line above a token (<c>L</c>), on the same line
    /// as the token that follows it (<c>I</c>), or after the token before it (<c>T</c>), so a comment that changes
    /// owner while keeping its order is caught too. Lines of a comment are trimmed, so re-indenting is not a loss;
    /// line terminators are ignored, and the trivia's own line ending too. Disabled text is compared exactly,
    /// because it was never parsed and its whitespace can be part of a string.
    /// </summary>
    private static IEnumerable<string> CodeAndComments(SyntaxTree tree)
    {
        foreach (SyntaxToken token in tree.GetRoot().DescendantTokens())
        {
            SyntaxTriviaList leading = token.LeadingTrivia;
            for (int i = 0; i < leading.Count; i++)
            {
                if (IsPreserved(leading[i]))
                {
                    yield return Describe(leading[i], FollowsOnTheSameLine(leading, i) ? "I" : "L");
                }
            }

            yield return $"{token.Kind()}:{token.Text}";

            foreach (SyntaxTrivia trivia in token.TrailingTrivia.Where(IsPreserved))
            {
                yield return Describe(trivia, "T");
            }
        }
    }

    private static bool FollowsOnTheSameLine(SyntaxTriviaList trivia, int index)
    {
        if (!trivia[index].IsKind(SyntaxKind.MultiLineCommentTrivia))
        {
            return false;
        }

        for (int i = index + 1; i < trivia.Count; i++)
        {
            if (trivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return false;
            }

            if (!trivia[i].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                return true;
            }
        }

        return true;
    }

    private static string Describe(SyntaxTrivia trivia, string position) =>
        trivia.IsKind(SyntaxKind.DisabledTextTrivia)
            ? "X:" + NormalizeTerminators(trivia.ToFullString()).TrimEnd('\n')
            : $"{position}:{Normalize(trivia)}";

    private static string NormalizeTerminators(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\u0085', '\n')
            .Replace('\u2028', '\n')
            .Replace('\u2029', '\n');

    private static bool IsPreserved(SyntaxTrivia trivia) =>
        trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.DisabledTextTrivia)
        || trivia.IsDirective
        || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);

    private static string Normalize(SyntaxTrivia trivia) =>
        string.Join('\n', NormalizeTerminators(trivia.ToFullString()).TrimEnd().Split('\n').Select(l => l.Trim()));
}
