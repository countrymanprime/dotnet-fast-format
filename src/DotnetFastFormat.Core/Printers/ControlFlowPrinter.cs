using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints <c>if</c>, <c>else</c>, <c>for</c>, <c>foreach</c>, <c>while</c> and <c>do</c> (ADR 0013).</summary>
internal static class ControlFlowPrinter
{
    /// <summary>Prints an <c>if</c> statement with its <c>else if</c> chain.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the statement is kept as written.</returns>
    public static Doc? If(IfStatementSyntax statement, NodeBuilder builder)
    {
        Doc head = Docs.Concat(
            builder.Token(statement.IfKeyword),
            Docs.Text(" "),
            StatementLayout.Parens(
                statement.OpenParenToken,
                ExpressionPrinter.Print(statement.Condition, builder, ExprContext.Condition),
                statement.CloseParenToken,
                builder));
        if (statement.Else is not { } clause)
        {
            return Docs.Concat(head, StatementLayout.Embedded(statement.Statement, endsTheParent: true));
        }

        Doc body = StatementLayout.Embedded(statement.Statement, endsTheParent: false);
        Doc? elsePart = Else(clause, builder);
        return elsePart is null ? null : Docs.Concat(head, body, Docs.HardLine, elsePart);
    }

    /// <summary>Prints a <c>for</c> statement.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc For(ForStatementSyntax statement, NodeBuilder builder)
    {
        Doc head = Docs.Concat(builder.Token(statement.ForKeyword), Docs.Text(" "));
        Doc first = builder.Token(statement.FirstSemicolonToken);
        Doc second = builder.Token(statement.SecondSemicolonToken);
        Doc body = StatementLayout.Embedded(statement.Statement, endsTheParent: true);
        bool hasInit = statement.Declaration is not null || statement.Initializers.Count > 0;
        bool hasCondition = statement.Condition is not null;
        bool hasIncrementors = statement.Incrementors.Count > 0;
        if (!hasInit && !hasCondition && !hasIncrementors)
        {
            return Docs.Concat(head, builder.Token(statement.OpenParenToken), first, second, builder.Token(statement.CloseParenToken), body);
        }

        Doc init = statement.Declaration is { } declaration
            ? DeclaratorPrinter.Declaration(declaration, [], builder)
            : ExpressionList(statement.Initializers, builder);
        Doc content = Docs.Concat(
            init,
            first,
            hasCondition || hasIncrementors ? Docs.Line : Docs.Empty,
            statement.Condition is null ? Docs.Empty : ExpressionPrinter.Print(statement.Condition, builder),
            second,
            hasIncrementors ? Docs.Concat(Docs.Line, ExpressionList(statement.Incrementors, builder)) : Docs.Empty);
        return Docs.Concat(head, StatementLayout.Parens(statement.OpenParenToken, content, statement.CloseParenToken, builder), body);
    }

    /// <summary>Prints a <c>foreach</c> statement.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc ForEach(CommonForEachStatementSyntax statement, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        if (!statement.AwaitKeyword.IsKind(SyntaxKind.None))
        {
            parts.Add(builder.Token(statement.AwaitKeyword));
            parts.Add(Docs.Text(" "));
        }

        parts.Add(builder.Token(statement.ForEachKeyword));
        parts.Add(Docs.Text(" "));
        Doc variable = statement switch
        {
            ForEachStatementSyntax each => Docs.Concat(builder.Tokens(each.Type), Docs.Text(" "), builder.Token(each.Identifier)),
            ForEachVariableStatementSyntax each => ExpressionPrinter.Print(each.Variable, builder),
            _ => throw new InvalidOperationException("Unknown foreach kind."),
        };
        Doc content = Docs.Concat(variable, Docs.Text(" "), builder.Token(statement.InKeyword), Docs.Text(" "), ExpressionPrinter.Print(statement.Expression, builder));
        parts.Add(StatementLayout.Parens(statement.OpenParenToken, content, statement.CloseParenToken, builder));
        parts.Add(StatementLayout.Embedded(statement.Statement, endsTheParent: true));
        return Docs.Concat(parts);
    }

    /// <summary>Prints a <c>while</c> statement.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc While(WhileStatementSyntax statement, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(statement.WhileKeyword),
            Docs.Text(" "),
            StatementLayout.Parens(
                statement.OpenParenToken,
                ExpressionPrinter.Print(statement.Condition, builder, ExprContext.Condition),
                statement.CloseParenToken,
                builder),
            StatementLayout.Embedded(statement.Statement, endsTheParent: true));

    /// <summary>Prints a <c>do</c> statement.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Do(DoStatementSyntax statement, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(statement.DoKeyword),
            StatementLayout.Embedded(statement.Statement, endsTheParent: false),
            Docs.HardLine,
            builder.Token(statement.WhileKeyword),
            Docs.Text(" "),
            StatementLayout.Parens(
                statement.OpenParenToken,
                ExpressionPrinter.Print(statement.Condition, builder, ExprContext.Condition),
                statement.CloseParenToken,
                builder),
            builder.Token(statement.SemicolonToken));

    private static Doc? Else(ElseClauseSyntax clause, NodeBuilder builder)
    {
        Doc keyword = builder.Token(clause.ElseKeyword);
        if (clause.Statement is not IfStatementSyntax chained)
        {
            return Docs.Concat(keyword, StatementLayout.Embedded(clause.Statement, endsTheParent: true));
        }

        PrintedNode printed = NodePrinter.Print(chained, trailingByAncestor: true);
        if (printed.Leading is { Lines.Count: > 0 })
        {
            // A comment between else and if has no place in the one-line form.
            builder.Reject();
            return null;
        }

        return Docs.Concat(keyword, Docs.Text(" "), printed.Doc);
    }

    private static Doc ExpressionList(SeparatedSyntaxList<ExpressionSyntax> list, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                parts.Add(builder.Token(list.GetSeparator(i - 1)));
                parts.Add(Docs.Text(" "));
            }

            parts.Add(ExpressionPrinter.Print(list[i], builder));
        }

        return Docs.Concat(parts);
    }
}
