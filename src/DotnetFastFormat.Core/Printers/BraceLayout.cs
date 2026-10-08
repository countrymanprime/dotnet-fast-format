using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints the braces of a body (ADR 0010).</summary>
internal static class BraceLayout
{
    /// <summary>
    /// Prints an opening brace that starts a line (Allman): the own-line comments and directives above it, such as an
    /// <c>#if</c> between a header and its body, then the brace, then a comment after it on its line.
    /// </summary>
    /// <param name="brace">The opening brace.</param>
    /// <param name="builder">Collects the brace's tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the trivia around the brace has a shape that is kept as written (the builder is then unsafe).</returns>
    public static OpenBrace? Open(SyntaxToken brace, NodeBuilder builder)
    {
        (bool clean, string? comment) = TriviaLines.ParseTrailing(brace.TrailingTrivia);
        LeadingTrivia? above = builder.IsFirst(brace)
            ? new LeadingTrivia([], BlankLineAfter: false)
            : TriviaLines.ParseLeading(brace.LeadingTrivia, atEndOfFile: false);
        if (!clean || above is null)
        {
            builder.Reject();
            return null;
        }

        builder.HandleLeading(brace);
        Doc token = builder.Token(brace, leadingHandled: true, trailingHandled: true);
        Doc doc = Docs.Concat(TriviaPrinter.Leading(above), token, comment is null ? Docs.Empty : Docs.LineSuffix(" " + comment));
        return new OpenBrace(doc, comment is not null, above.Lines.Count > 0);
    }

    /// <summary>
    /// Returns whether a body printed <c>{ }</c> on one line: it holds nothing, and no comment or directive sits before the
    /// closing brace, after the opening brace, or (when the brace starts its own line) before it.
    /// </summary>
    /// <param name="open">The opening brace.</param>
    /// <param name="close">The closing brace.</param>
    /// <param name="openIsFirst">Whether the opening brace is the first token of the node, so the list prints what is above it.</param>
    /// <returns><see langword="true"/> when the body is empty and plain.</returns>
    public static bool IsEmpty(SyntaxToken open, SyntaxToken close, bool openIsFirst) =>
        TriviaLines.ParseLeading(close.LeadingTrivia, atEndOfFile: false) is { Lines.Count: 0 }
        && TriviaLines.ParseTrailing(open.TrailingTrivia) is (true, null)
        && (openIsFirst || !TriviaRules.HasCommentOrDirective(open.LeadingTrivia));
}
