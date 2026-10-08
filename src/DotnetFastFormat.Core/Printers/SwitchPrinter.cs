using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints <c>switch</c> statements (ADR 0013): the braces on their own lines, each section's labels on their own lines,
/// and the section's statements indented one level (a lone block stays at the labels' indent).
/// </summary>
internal static class SwitchPrinter
{
    /// <summary>Prints a switch statement.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the trivia before its closing brace is in a position that is kept as written.</returns>
    public static Doc? Switch(SwitchStatementSyntax statement, NodeBuilder builder)
    {
        LeadingTrivia? closing = TriviaLines.ParseLeading(statement.CloseBraceToken.LeadingTrivia, atEndOfFile: false);
        if (closing is null)
        {
            builder.Reject();
            return null;
        }

        Doc head = Docs.Concat(builder.Token(statement.SwitchKeyword), Docs.Text(" "));
        Doc governing = statement.OpenParenToken.IsKind(SyntaxKind.None)
            ? ExpressionPrinter.Print(statement.Expression, builder)
            : StatementLayout.Parens(statement.OpenParenToken, ExpressionPrinter.Print(statement.Expression, builder), statement.CloseParenToken, builder);
        Doc open = builder.Token(statement.OpenBraceToken);
        Doc close = builder.Token(statement.CloseBraceToken, leadingHandled: true);
        if (statement.Sections.Count == 0 && closing.Lines.Count == 0)
        {
            return Docs.Concat(head, governing, Docs.Text(" "), open, Docs.Text(" "), close);
        }

        return Docs.Concat(
            head,
            governing,
            Docs.HardLine,
            open,
            Docs.Indent(Docs.Concat(Docs.HardLine, MemberList.Print([.. statement.Sections], closing))),
            Docs.HardLine,
            close);
    }

    /// <summary>Prints one section: its labels, then its statements.</summary>
    /// <param name="section">The section.</param>
    /// <param name="builder">Collects the section's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Section(SwitchSectionSyntax section, NodeBuilder builder)
    {
        var labels = new List<Doc>();
        foreach (SwitchLabelSyntax label in section.Labels)
        {
            labels.Add(Label(label, builder));
        }

        Doc head = Docs.Join(Docs.HardLine, labels);
        if (section.Statements is [BlockSyntax block])
        {
            return Docs.Concat(head, Docs.HardLine, NodePrinter.PrintAlone(block, trailingByAncestor: true));
        }

        return Docs.Concat(
            head,
            Docs.Indent(Docs.Concat(Docs.HardLine, MemberList.Print([.. section.Statements], closing: null, lastTrailingByAncestor: true))));
    }

    private static Doc Label(SwitchLabelSyntax label, NodeBuilder builder) => label switch
    {
        CaseSwitchLabelSyntax @case => Docs.Concat(
            builder.Token(@case.Keyword),
            Docs.Text(" "),
            ExpressionPrinter.Print(@case.Value, builder),
            builder.Token(@case.ColonToken)),
        CasePatternSwitchLabelSyntax pattern => Docs.Concat(
            builder.Token(pattern.Keyword),
            Docs.Text(" "),
            builder.Verbatim(pattern.Pattern),
            Guard(pattern.WhenClause, builder),
            builder.Token(pattern.ColonToken)),
        _ => Docs.Concat(builder.Token(label.Keyword), builder.Token(label.ColonToken)),
    };

    private static Doc Guard(WhenClauseSyntax? clause, NodeBuilder builder) =>
        clause is null
            ? Docs.Empty
            : Docs.Concat(Docs.Text(" "), builder.Token(clause.WhenKeyword), Docs.Text(" "), ExpressionPrinter.Print(clause.Condition, builder));
}
