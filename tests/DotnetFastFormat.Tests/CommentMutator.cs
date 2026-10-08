using DotnetFastFormat.Core;
using Microsoft.CodeAnalysis;

namespace DotnetFastFormat.Tests;

/// <summary>
/// Inserts comments and directives at token boundaries of a file, formats the result and checks every invariant.
/// The insertions land in every position the trivia rules of ADR 0010 distinguish (and many they do not), so a
/// position the printers wrongly accept shows up as a lost, moved or duplicated comment.
/// </summary>
internal static class CommentMutator
{
    private static readonly string[] Names = ["inline", "own-line", "after-token", "directive", "conditional-pair"];

    /// <summary>Mutates and formats <paramref name="source"/> <paramref name="perKind"/> times for each kind of insertion.</summary>
    /// <param name="source">The file's text.</param>
    /// <param name="perKind">How many insertions of each kind.</param>
    /// <param name="seed">Makes the choice of positions repeatable.</param>
    /// <param name="options">The formatter settings, or null for the defaults.</param>
    /// <returns>The first failure with its input, or <see langword="null"/>, and the number of failures and of files formatted.</returns>
    public static (string? First, int Failures, int Formatted) Run(string source, int perKind, int seed, FormatOptions? options = null)
    {
        var random = new Random(seed);
        SyntaxToken[] tokens = [.. SourceParser.Parse(source).GetRoot().DescendantTokens()];
        string? first = null;
        int failures = 0;
        int formatted = 0;

        if (tokens.Length < 2)
        {
            return (null, 0, 0);
        }

        foreach (string name in Names)
        {
            for (int i = 0; i < perKind; i++)
            {
                string mutated = Mutate(source, tokens, name, random);
                try
                {
                    Invariants.FormatAndCheck(new RoslynFormatter(), mutated, options);
                    formatted++;
                }
                catch (FormatterException)
                {
                    // The insertion broke the file (an unbalanced region, a comment in a string hole): nothing to format.
                }
                catch (Exception ex)
                {
                    failures++;
                    first ??= $"{name}: {ex.GetType().Name}: {ex.Message}\n--- mutated input ---\n{mutated}";
                }
            }
        }

        return (first, failures, formatted);
    }

    private static string Mutate(string source, SyntaxToken[] tokens, string name, Random random)
    {
        SyntaxToken token = tokens[random.Next(tokens.Length - 1)];
        return name switch
        {
            "inline" => source.Insert(token.SpanStart, "/* c */ "),
            "own-line" => source.Insert(token.SpanStart, random.Next(2) == 0 ? "// c\n" : "\n// c\n\n"),
            "after-token" => source.Insert(token.Span.End, " // t\n"),
            "directive" => source.Insert(token.SpanStart, "\n#pragma warning disable CS0168\n"),
            _ => InsertConditionalPair(source, tokens, random),
        };
    }

    private static string InsertConditionalPair(string source, SyntaxToken[] tokens, Random random)
    {
        int a = random.Next(tokens.Length);
        int b = random.Next(tokens.Length);
        (int low, int high) = a <= b ? (a, b) : (b, a);
        return source
            .Insert(tokens[high].SpanStart, "\n#endif\n")
            .Insert(tokens[low].SpanStart, "\n#if NEVER_DEFINED\n");
    }
}
