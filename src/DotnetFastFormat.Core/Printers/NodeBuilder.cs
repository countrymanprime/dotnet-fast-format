using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Collects the documents of one node's own tokens and notes whether printing them would lose trivia.
/// When it would, the caller prints the whole node verbatim instead (ADR 0008, ADR 0010). The trivia before the
/// node's first token and after its last token is the list's to print, so the builder does not count it.
/// </summary>
internal sealed class NodeBuilder
{
    private readonly SyntaxToken first;
    private readonly SyntaxToken last;

    /// <summary>Initializes a new instance of the <see cref="NodeBuilder"/> class.</summary>
    /// <param name="node">The node being printed.</param>
    /// <param name="leadingPrinted">Whether the list prints the trivia before the node's first token.</param>
    /// <param name="trailingPrinted">Whether the list prints the trivia after the node's last token. It does not for a file-scoped namespace, whose last token belongs to its last member.</param>
    public NodeBuilder(SyntaxNode node, bool leadingPrinted = true, bool trailingPrinted = true)
    {
        first = leadingPrinted ? node.GetFirstToken() : default;
        last = trailingPrinted ? node.GetLastToken() : default;
    }

    /// <summary>Gets a value indicating whether every token handed to the builder can be printed from its text alone.</summary>
    public bool IsSafe { get; private set; } = true;

    /// <summary>Returns whether <paramref name="token"/> carries a comment or directive that no list will print.</summary>
    /// <param name="token">The token to inspect.</param>
    /// <param name="leadingHandled">Whether the caller prints the token's leading trivia itself, as for a closing brace.</param>
    /// <param name="trailingHandled">Whether the caller prints the token's trailing trivia itself, as for the semicolon of a file-scoped namespace.</param>
    /// <returns><see langword="true"/> when printing the token from its text would drop trivia.</returns>
    public bool HasUnprintedTrivia(SyntaxToken token, bool leadingHandled = false, bool trailingHandled = false) =>
        (token != first && !leadingHandled && token.LeadingTrivia.Any(TriviaRules.IsCommentOrDirective))
        || (token != last && !trailingHandled && token.TrailingTrivia.Any(TriviaRules.IsCommentOrDirective));

    /// <summary>Prints one token of the node.</summary>
    /// <param name="token">The token.</param>
    /// <param name="leadingHandled">Whether the caller prints the token's leading trivia itself, as for a closing brace.</param>
    /// <param name="trailingHandled">Whether the caller prints the token's trailing trivia itself.</param>
    /// <returns>A text document with the token's text.</returns>
    public Doc Token(SyntaxToken token, bool leadingHandled = false, bool trailingHandled = false)
    {
        Check(token, leadingHandled, trailingHandled);
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

    /// <summary>
    /// Copies a child of the node as written. The trivia after the node's last token is left out when the child ends
    /// there, because the list prints it. A child that ends in a <c>//</c> comment and is followed by more of the node
    /// would swallow what follows, so the node is marked unsafe instead.
    /// </summary>
    /// <param name="child">The child to copy.</param>
    /// <returns>A verbatim document.</returns>
    public Doc Verbatim(SyntaxNode child)
    {
        string text = child.ToFullString();
        SyntaxToken end = child.GetLastToken();
        if (end == last)
        {
            text = text[..^end.TrailingTrivia.FullSpan.Length];
        }
        else if (end.TrailingTrivia.Any(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineCommentTrivia)))
        {
            IsSafe = false;
        }

        return Docs.Verbatim(text.Trim());
    }

    /// <summary>Marks the node unsafe, for syntax the printer chooses not to handle.</summary>
    public void Reject() => IsSafe = false;

    private void Check(SyntaxToken token, bool leadingHandled, bool trailingHandled)
    {
        if (HasUnprintedTrivia(token, leadingHandled, trailingHandled)
            || token.Text.Contains('\n', StringComparison.Ordinal)
            || token.Text.Contains('\r', StringComparison.Ordinal))
        {
            IsSafe = false;
        }
    }
}
