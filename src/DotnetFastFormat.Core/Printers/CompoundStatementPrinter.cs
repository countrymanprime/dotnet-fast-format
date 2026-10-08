using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints <c>using</c>, <c>lock</c>, <c>fixed</c>, <c>checked</c>, <c>unsafe</c> and <c>try</c> statements (ADR 0013).</summary>
internal static class CompoundStatementPrinter
{
    /// <summary>Prints a <c>using</c> statement; a <c>using</c> that is the body of a <c>using</c> stays at the same indent.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Using(UsingStatementSyntax statement, NodeBuilder builder)
    {
        var head = new List<Doc>();
        if (!statement.AwaitKeyword.IsKind(SyntaxKind.None))
        {
            head.Add(builder.Token(statement.AwaitKeyword));
            head.Add(Docs.Text(" "));
        }

        head.Add(builder.Token(statement.UsingKeyword));
        head.Add(Docs.Text(" "));
        Doc content = statement.Declaration is { } declaration
            ? DeclaratorPrinter.Declaration(declaration, [], builder)
            : ExpressionPrinter.Print(statement.Expression!, builder);
        head.Add(StatementLayout.Parens(statement.OpenParenToken, content, statement.CloseParenToken, builder));
        head.Add(StatementLayout.Embedded(statement.Statement, endsTheParent: true));
        return Docs.Concat(head);
    }

    /// <summary>Prints <c>lock (x) body</c>.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Lock(LockStatementSyntax statement, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(statement.LockKeyword),
            Docs.Text(" "),
            StatementLayout.Parens(statement.OpenParenToken, ExpressionPrinter.Print(statement.Expression, builder), statement.CloseParenToken, builder),
            StatementLayout.Embedded(statement.Statement, endsTheParent: true));

    /// <summary>Prints <c>fixed (declaration) body</c>.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Fixed(FixedStatementSyntax statement, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(statement.FixedKeyword),
            Docs.Text(" "),
            StatementLayout.Parens(statement.OpenParenToken, DeclaratorPrinter.Declaration(statement.Declaration, [], builder), statement.CloseParenToken, builder),
            StatementLayout.Embedded(statement.Statement, endsTheParent: true));

    /// <summary>Prints <c>checked { }</c> and <c>unsafe { }</c> statements.</summary>
    /// <param name="keyword">The keyword token.</param>
    /// <param name="block">The block.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc KeywordBlock(SyntaxToken keyword, BlockSyntax block, NodeBuilder builder) =>
        Docs.Concat(builder.Token(keyword), StatementLayout.Embedded(block, endsTheParent: true));

    /// <summary>Prints <c>try</c> with its <c>catch</c> and <c>finally</c> clauses, each on its own line.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Try(TryStatementSyntax statement, NodeBuilder builder)
    {
        bool hasFinally = statement.Finally is not null;
        var parts = new List<Doc>
        {
            builder.Token(statement.TryKeyword),
            StatementLayout.Embedded(statement.Block, endsTheParent: false),
        };
        for (int i = 0; i < statement.Catches.Count; i++)
        {
            bool last = !hasFinally && i == statement.Catches.Count - 1;
            parts.Add(Docs.HardLine);
            parts.Add(Catch(statement.Catches[i], last, builder));
        }

        if (statement.Finally is { } clause)
        {
            parts.Add(Docs.HardLine);
            parts.Add(builder.Token(clause.FinallyKeyword));
            parts.Add(StatementLayout.Embedded(clause.Block, endsTheParent: true));
        }

        // A try with only a block is a syntax error, so there is always a clause above; the last block ends the statement.
        return Docs.Concat(parts);
    }

    private static Doc Catch(CatchClauseSyntax clause, bool endsTheStatement, NodeBuilder builder)
    {
        var parts = new List<Doc> { builder.Token(clause.CatchKeyword) };
        if (clause.Declaration is { } declaration)
        {
            parts.Add(Docs.Text(" "));
            Doc content = Docs.Concat(
                builder.Tokens(declaration.Type),
                declaration.Identifier.IsKind(SyntaxKind.None) ? Docs.Empty : Docs.Concat(Docs.Text(" "), builder.Token(declaration.Identifier)));
            parts.Add(StatementLayout.Parens(declaration.OpenParenToken, content, declaration.CloseParenToken, builder));
        }

        if (clause.Filter is { } filter)
        {
            parts.Add(Docs.Text(" "));
            parts.Add(builder.Token(filter.WhenKeyword));
            parts.Add(Docs.Text(" "));
            parts.Add(StatementLayout.Parens(filter.OpenParenToken, ExpressionPrinter.Print(filter.FilterExpression, builder, ExprContext.Condition), filter.CloseParenToken, builder));
        }

        parts.Add(StatementLayout.Embedded(clause.Block, endsTheParent: endsTheStatement));
        return Docs.Concat(parts);
    }
}
