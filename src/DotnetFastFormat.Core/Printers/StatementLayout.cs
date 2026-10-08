using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>The pieces every compound statement shares: a parenthesized header and an embedded body (ADR 0013, ADR 0014).</summary>
internal static class StatementLayout
{
    /// <summary>
    /// Prints <c>(content)</c> as one group: on the line when it fits, otherwise the content on its own indented line
    /// and <c>)</c> on the line after it.
    /// </summary>
    /// <param name="open">The opening parenthesis.</param>
    /// <param name="content">The content.</param>
    /// <param name="close">The closing parenthesis.</param>
    /// <param name="builder">Collects the parentheses' tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Parens(SyntaxToken open, Doc content, SyntaxToken close, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(open),
            Docs.Group(Docs.Concat(Docs.Indent(Docs.Concat(Docs.SoftLine, content)), Docs.SoftLine)),
            builder.Token(close));

    /// <summary>
    /// Prints the body of a compound statement below its header: a block at the statement's indent, any other statement
    /// indented one level, and an empty block as <c>{ }</c> on the header's line.
    /// </summary>
    /// <param name="body">The statement.</param>
    /// <param name="endsTheParent">Whether the body ends on the same token as the statement around it, which then prints the comment after it.</param>
    /// <returns>The document, starting with its line break.</returns>
    public static Doc Embedded(StatementSyntax body, bool endsTheParent)
    {
        PrintedNode printed = NodePrinter.Print(body, trailingByAncestor: endsTheParent);
        Doc doc = Docs.Concat(
            TriviaPrinter.Leading(printed.Leading),
            printed.Doc,
            printed.TrailingComment is { } comment ? Docs.LineSuffix(" " + comment) : Docs.Empty);
        if (body is BlockSyntax block)
        {
            return printed.Leading is { Lines.Count: 0 } && StatementPrinter.IsEmpty(block, openIsFirst: true)
                ? Docs.Concat(Docs.Text(" "), doc)
                : Docs.Concat(Docs.HardLine, doc);
        }

        // using (a) using (b) { } is a stack, not a nesting.
        return body is UsingStatementSyntax && body.Parent is UsingStatementSyntax
            ? Docs.Concat(Docs.HardLine, doc)
            : Docs.Indent(Docs.Concat(Docs.HardLine, doc));
    }
}
