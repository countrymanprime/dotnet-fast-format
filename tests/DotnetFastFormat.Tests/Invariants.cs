using DotnetFastFormat.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Tests;

/// <summary>
/// The single assertion every test layer calls, so no test can format without also checking the
/// invariants (see the <c>formatter-verification</c> skill).
/// </summary>
public static class Invariants
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    /// <summary>Formats <paramref name="source"/>, checks every invariant, and returns the output.</summary>
    /// <exception cref="InvariantViolationException">An invariant does not hold.</exception>
    public static string FormatAndCheck(IFormatter formatter, string source)
    {
        string output = formatter.Format(source);
        SyntaxTree input = CSharpSyntaxTree.ParseText(source, ParseOptions);
        SyntaxTree result = CSharpSyntaxTree.ParseText(output, ParseOptions);

        CheckValidOutput(input, result);
        CheckTreePreserving(input, result);
        CheckNoLoss(input, result);

        string second = formatter.Format(output);
        if (!string.Equals(second, output, StringComparison.Ordinal))
        {
            throw new InvariantViolationException(Invariant.Idempotent, "Formatting the output again changed it.");
        }

        return output;
    }

    private static void CheckValidOutput(SyntaxTree input, SyntaxTree output)
    {
        Dictionary<string, int> before = CountErrors(input);
        foreach ((string id, int count) in CountErrors(output))
        {
            if (count > before.GetValueOrDefault(id))
            {
                throw new InvariantViolationException(Invariant.ValidOutput, $"Output has new diagnostic {id}.");
            }
        }
    }

    private static Dictionary<string, int> CountErrors(SyntaxTree tree) =>
        tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .GroupBy(d => d.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

    private static void CheckTreePreserving(SyntaxTree input, SyntaxTree output)
    {
        List<string> expected = Tokens(input).ToList();
        List<string> actual = Tokens(output).ToList();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new InvariantViolationException(Invariant.TreePreserving, "Output tokens differ from input tokens.");
        }
    }

    private static void CheckNoLoss(SyntaxTree input, SyntaxTree output)
    {
        List<string> expected = CodeAndComments(input).ToList();
        List<string> actual = CodeAndComments(output).ToList();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new InvariantViolationException(
                Invariant.NoLoss, "A comment or directive was dropped, changed or moved across code.");
        }
    }

    private static IEnumerable<string> Tokens(SyntaxTree tree) =>
        tree.GetRoot().DescendantTokens().Select(t => $"{t.Kind()}:{t.Text}");

    /// <summary>
    /// Code tokens and comment/directive trivia in source order, so a comment that moves across a
    /// token changes the sequence. Each line is trimmed, so re-indenting is not a loss.
    /// </summary>
    private static IEnumerable<string> CodeAndComments(SyntaxTree tree)
    {
        foreach (SyntaxToken token in tree.GetRoot().DescendantTokens())
        {
            foreach (SyntaxTrivia trivia in token.LeadingTrivia)
            {
                if (IsPreserved(trivia))
                {
                    yield return Normalize(trivia);
                }
            }

            yield return $"{token.Kind()}:{token.Text}";

            foreach (SyntaxTrivia trivia in token.TrailingTrivia)
            {
                if (IsPreserved(trivia))
                {
                    yield return Normalize(trivia);
                }
            }
        }
    }

    private static bool IsPreserved(SyntaxTrivia trivia) =>
        trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.DisabledTextTrivia)
        || trivia.IsDirective
        || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);

    private static string Normalize(SyntaxTrivia trivia) =>
        string.Join('\n', trivia.ToFullString().Split('\n').Select(l => l.Trim()));
}
