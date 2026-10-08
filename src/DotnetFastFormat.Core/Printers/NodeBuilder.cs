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
    private List<SyntaxToken>? handledLeading;
    private List<SyntaxToken>? handledTrailing;

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

    /// <summary>
    /// Records that the caller prints the leading trivia of <paramref name="token"/> itself, as own-line items above it
    /// (an attribute list's successor, an opening brace that starts its line).
    /// </summary>
    /// <param name="token">The token whose leading trivia the caller prints.</param>
    public void HandleLeading(SyntaxToken token) => (handledLeading ??= []).Add(token);

    /// <summary>Records that the caller prints the comment after <paramref name="token"/> itself, on the same line.</summary>
    /// <param name="token">The token whose trailing trivia the caller prints.</param>
    public void HandleTrailing(SyntaxToken token) => (handledTrailing ??= []).Add(token);

    /// <summary>Returns whether the list prints the trivia before <paramref name="token"/>, because it is the first token of the node.</summary>
    /// <param name="token">The token.</param>
    /// <returns><see langword="true"/> for the node's first token.</returns>
    public bool IsFirst(SyntaxToken token) => token == first;

    /// <summary>Returns whether <paramref name="token"/> carries a comment or directive that no list will print.</summary>
    /// <param name="token">The token to inspect.</param>
    /// <param name="leadingHandled">Whether the caller prints the token's leading trivia itself, as for a closing brace.</param>
    /// <param name="trailingHandled">Whether the caller prints the token's trailing trivia itself, as for the semicolon of a file-scoped namespace.</param>
    /// <returns><see langword="true"/> when printing the token from its text would drop trivia.</returns>
    public bool HasUnprintedTrivia(SyntaxToken token, bool leadingHandled = false, bool trailingHandled = false) =>
        (token != first && !leadingHandled && handledLeading?.Contains(token) != true && TriviaRules.HasCommentOrDirective(token.LeadingTrivia))
        || (token != last && !trailingHandled && handledTrailing?.Contains(token) != true && TriviaRules.HasCommentOrDirective(token.TrailingTrivia));

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
    /// Prints a token that may hold line breaks (a verbatim, raw or interpolated string, which must never be
    /// reflowed): its text is emitted exactly as written.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>A text document, or a verbatim document when the token spans lines.</returns>
    public Doc Literal(SyntaxToken token)
    {
        if (HasUnprintedTrivia(token))
        {
            IsSafe = false;
        }

        return token.Text.AsSpan().IndexOfAny('\r', '\n') >= 0 ? Docs.Verbatim(token.Text, forcesBreak: false) : Docs.Text(token.Text);
    }

    /// <summary>
    /// Copies a child of the node as written. The trivia the list prints is left out: the trivia before the node's
    /// first token when the child starts there, and the trivia after the node's last token when the child ends there.
    /// A child that ends in a <c>//</c> comment and is followed by more of the node would swallow what follows, so the
    /// node is marked unsafe instead.
    /// </summary>
    /// <param name="child">The child to copy.</param>
    /// <param name="startsOwnLine">Whether the child is printed on a line of its own, so a comment above it stays above it. Otherwise a comment in its leading trivia would move onto the previous token's line, and the node is marked unsafe.</param>
    /// <param name="forcesBreak">Whether a line break in the copied text makes the enclosing groups break; <see langword="false"/> for a string.</param>
    /// <param name="trailingHandled">Whether the caller prints the comment after the child's last token itself, so the copy stops at the token.</param>
    /// <returns>A verbatim document.</returns>
    public Doc Verbatim(SyntaxNode child, bool startsOwnLine = false, bool forcesBreak = true, bool trailingHandled = false)
    {
        string text = child.ToFullString();
        SyntaxToken start = child.GetFirstToken();
        int from = 0;
        if (start == first || handledLeading?.Contains(start) == true)
        {
            from = start.Span.Start - child.FullSpan.Start;
        }
        else if (!startsOwnLine && TriviaRules.HasCommentOrDirective(start.LeadingTrivia))
        {
            IsSafe = false;
        }

        SyntaxToken end = child.GetLastToken();
        int to = text.Length;
        if (end == last || trailingHandled)
        {
            to -= end.TrailingTrivia.FullSpan.Length;
        }
        else if (HasLineComment(end.TrailingTrivia))
        {
            IsSafe = false;
        }

        return Docs.Verbatim(to > from ? text[from..to].Trim() : string.Empty, forcesBreak);
    }

    /// <summary>Marks the node unsafe, for syntax the printer chooses not to handle.</summary>
    public void Reject() => IsSafe = false;

    private static bool HasLineComment(SyntaxTriviaList trivia)
    {
        foreach (SyntaxTrivia piece in trivia)
        {
            if (piece.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineCommentTrivia))
            {
                return true;
            }
        }

        return false;
    }

    private void Check(SyntaxToken token, bool leadingHandled, bool trailingHandled)
    {
        if (HasUnprintedTrivia(token, leadingHandled, trailingHandled) || token.Text.AsSpan().IndexOfAny('\r', '\n') >= 0)
        {
            IsSafe = false;
        }
    }
}
