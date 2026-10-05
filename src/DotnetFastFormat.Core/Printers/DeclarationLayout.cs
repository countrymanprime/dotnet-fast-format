using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Lays out a method or constructor: signature, optional initializer, then its body.</summary>
internal static class DeclarationLayout
{
    /// <summary>Puts the pieces of a method or constructor together.</summary>
    /// <param name="head">Modifiers, return type, name and parameter list.</param>
    /// <param name="wrapped">Documents that move to their own indented lines when the signature does not fit (constraint clauses).</param>
    /// <param name="initializer">A constructor initializer printed on its own indented line, or <see langword="null"/>.</param>
    /// <param name="kind">How the declaration ends.</param>
    /// <param name="body">The text of the ending, in the form <paramref name="kind"/> says.</param>
    /// <returns>The document.</returns>
    public static Doc Compose(Doc head, IReadOnlyList<Doc> wrapped, Doc? initializer, MemberBodyKind kind, Doc body)
    {
        Doc declaration = Docs.Concat(head, Docs.Indent(Docs.Concat(wrapped)));
        Doc initializerLine = initializer is null ? Docs.Empty : Docs.Indent(Docs.Concat(Docs.HardLine, initializer));
        switch (kind)
        {
            case MemberBodyKind.Block:
                return Docs.Concat(Docs.Group(declaration), initializerLine, Docs.HardLine, body);
            case MemberBodyKind.EmptyBlock when initializer is null:
                // An empty body stays on the signature's last line, or goes on its own line when the signature wrapped.
                return Docs.Group(Docs.Concat(
                    declaration,
                    Docs.IfBreak(Docs.Concat(Docs.HardLine, body), Docs.Concat(Docs.Text(" "), body))));
            case MemberBodyKind.EmptyBlock:
                return Docs.Concat(Docs.Group(declaration), initializerLine, Docs.Text(" "), body);
            default:
                return Docs.Concat(Docs.Group(declaration), initializerLine, body);
        }
    }

    /// <summary>Prints the ending of a method or constructor.</summary>
    /// <param name="block">The block body, or <see langword="null"/>.</param>
    /// <param name="expression">The expression body, or <see langword="null"/>.</param>
    /// <param name="semicolon">The semicolon token of a declaration with no block.</param>
    /// <param name="builder">Collects the declaration's own tokens.</param>
    /// <returns>The kind of ending and its document.</returns>
    public static (MemberBodyKind Kind, Doc Body) Body(
        BlockSyntax? block,
        ArrowExpressionClauseSyntax? expression,
        SyntaxToken semicolon,
        NodeBuilder builder)
    {
        if (block is not null)
        {
            bool empty = block.Statements.Count == 0
                && !TriviaRules.CarriesCommentOrDirective(block.OpenBraceToken)
                && !TriviaRules.CarriesCommentOrDirective(block.CloseBraceToken);
            return empty
                ? (MemberBodyKind.EmptyBlock, Docs.Text("{ }"))
                : (MemberBodyKind.Block, VerbatimText.Of(block));
        }

        if (expression is not null)
        {
            return (
                MemberBodyKind.Expression,
                Docs.Concat(
                    Docs.Text(" "),
                    builder.Token(expression.ArrowToken),
                    Docs.Text(" "),
                    VerbatimText.Of(expression.Expression),
                    builder.Token(semicolon)));
        }

        return (MemberBodyKind.Semicolon, builder.Token(semicolon));
    }
}
