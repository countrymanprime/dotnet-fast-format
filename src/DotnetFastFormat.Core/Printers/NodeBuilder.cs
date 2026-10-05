using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Collects the documents of one node's own tokens and notes whether printing them would lose trivia.
/// When it would, the caller prints the whole node verbatim instead (ADR 0008).
/// </summary>
internal sealed class NodeBuilder
{
    /// <summary>Gets a value indicating whether every token handed to the builder can be printed from its text alone.</summary>
    public bool IsSafe { get; private set; } = true;

    /// <summary>Prints one token of the node.</summary>
    /// <param name="token">The token.</param>
    /// <returns>A text document with the token's text.</returns>
    public Doc Token(SyntaxToken token)
    {
        Check(token);
        return Docs.Text(token.Text);
    }

    /// <summary>Prints consecutive tokens with the spacing of <see cref="TokenSpacing"/>.</summary>
    /// <param name="tokens">The tokens, in source order.</param>
    /// <returns>The joined document.</returns>
    public Doc Tokens(IEnumerable<SyntaxToken> tokens)
    {
        var parts = new List<Doc>();
        SyntaxToken previous = default;
        foreach (SyntaxToken token in tokens)
        {
            if (previous != default && TokenSpacing.NeedsSpace(previous, token))
            {
                parts.Add(Docs.Text(" "));
            }

            parts.Add(Token(token));
            previous = token;
        }

        return Docs.Concat(parts);
    }

    /// <summary>Prints all tokens of <paramref name="node"/>, which must contain nothing but headers and names.</summary>
    /// <param name="node">The node, or <see langword="null"/> for nothing.</param>
    /// <returns>The joined document.</returns>
    public Doc Tokens(SyntaxNode? node) => node is null ? Docs.Empty : Tokens(node.DescendantTokens());

    /// <summary>Marks the node unsafe, for syntax the printer chooses not to handle.</summary>
    public void Reject() => IsSafe = false;

    private void Check(SyntaxToken token)
    {
        if (TriviaRules.CarriesCommentOrDirective(token)
            || token.Text.Contains('\n', StringComparison.Ordinal)
            || token.Text.Contains('\r', StringComparison.Ordinal))
        {
            IsSafe = false;
        }
    }
}
