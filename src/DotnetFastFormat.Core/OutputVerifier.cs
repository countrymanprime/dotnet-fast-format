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

        if (!SameTokens(before, after))
        {
            return Fail(OutputInvariant.TreePreserving, "Output tokens differ from input tokens.");
        }

        if (!SameComments(before, after))
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

    private static Dictionary<string, int> CountErrors(SyntaxTree tree)
    {
        // A tree with no diagnostics at all (the usual case) needs no walk.
        if (!tree.GetRoot().ContainsDiagnostics)
        {
            return [];
        }

        return tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .GroupBy(d => d.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
    }

    /// <summary>Whether both trees hold the same tokens, by kind and text, in the same order. Allocates nothing per token.</summary>
    private static bool SameTokens(SyntaxTree before, SyntaxTree after)
    {
        using IEnumerator<SyntaxToken> left = before.GetRoot().DescendantTokens().GetEnumerator();
        using IEnumerator<SyntaxToken> right = after.GetRoot().DescendantTokens().GetEnumerator();
        while (true)
        {
            bool hasLeft = left.MoveNext();
            if (hasLeft != right.MoveNext())
            {
                return false;
            }

            if (!hasLeft)
            {
                return true;
            }

            if (left.Current.RawKind != right.Current.RawKind || !string.Equals(left.Current.Text, right.Current.Text, StringComparison.Ordinal))
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Whether the comments, documentation comments, directives and disabled text of both trees are the same, token by
    /// token, so a comment that moves across a token or changes owner is a difference. The tokens are known to be equal.
    /// Each comment also records where it sits: on its own line above a token (<c>L</c>), on the same line
    /// as the token that follows it (<c>I</c>), or after the token before it (<c>T</c>). Lines of a comment are trimmed, so re-indenting is not a loss;
    /// line terminators are ignored, and the trivia's own line ending too. Disabled text is compared exactly,
    /// because it was never parsed and its whitespace can be part of a string.
    /// </summary>
    private static bool SameComments(SyntaxTree before, SyntaxTree after)
    {
        using IEnumerator<SyntaxToken> left = before.GetRoot().DescendantTokens().GetEnumerator();
        using IEnumerator<SyntaxToken> right = after.GetRoot().DescendantTokens().GetEnumerator();
        while (left.MoveNext() && right.MoveNext())
        {
            if (!SameTrivia(left.Current.LeadingTrivia, right.Current.LeadingTrivia, leading: true)
                || !SameTrivia(left.Current.TrailingTrivia, right.Current.TrailingTrivia, leading: false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameTrivia(SyntaxTriviaList x, SyntaxTriviaList y, bool leading)
    {
        int i = 0;
        int j = 0;
        while (true)
        {
            while (i < x.Count && !IsPreserved(x[i]))
            {
                i++;
            }

            while (j < y.Count && !IsPreserved(y[j]))
            {
                j++;
            }

            if (i == x.Count || j == y.Count)
            {
                return i == x.Count && j == y.Count;
            }

            string left = Describe(x[i], leading ? (FollowsOnTheSameLine(x, i) ? "I" : "L") : "T");
            string right = Describe(y[j], leading ? (FollowsOnTheSameLine(y, j) ? "I" : "L") : "T");
            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                return false;
            }

            i++;
            j++;
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
