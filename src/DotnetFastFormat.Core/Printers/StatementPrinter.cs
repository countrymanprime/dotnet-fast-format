using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints statements (ADR 0013). A statement kind with no printer is rejected, which copies that statement as
/// written and nothing larger (ADR 0014).
/// </summary>
internal static class StatementPrinter
{
    /// <summary>Prints a top-level statement.</summary>
    /// <param name="global">The top-level statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the statement is kept as written.</returns>
    public static Doc? Global(GlobalStatementSyntax global, NodeBuilder builder)
    {
        if (global.AttributeLists.Count > 0 || global.Modifiers.Count > 0)
        {
            builder.Reject();
            return null;
        }

        return Print(global.Statement, builder);
    }

    /// <summary>Prints a statement.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="builder">Collects the statement's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the statement is kept as written.</returns>
    public static Doc? Print(StatementSyntax statement, NodeBuilder builder)
    {
        Doc? doc = statement switch
        {
            BlockSyntax block => Block(block, builder),
            ExpressionStatementSyntax expression => Concat(ExpressionPrinter.Print(expression.Expression, builder, ExprContext.Statement), builder.Token(expression.SemicolonToken)),
            LocalDeclarationStatementSyntax local => LocalDeclaration(local, builder),
            ReturnStatementSyntax @return => Keyword(@return.ReturnKeyword, @return.Expression, @return.SemicolonToken, builder),
            ThrowStatementSyntax @throw => Keyword(@throw.ThrowKeyword, @throw.Expression, @throw.SemicolonToken, builder),
            YieldStatementSyntax yield => Yield(yield, builder),
            BreakStatementSyntax @break => Concat(builder.Token(@break.BreakKeyword), builder.Token(@break.SemicolonToken)),
            ContinueStatementSyntax @continue => Concat(builder.Token(@continue.ContinueKeyword), builder.Token(@continue.SemicolonToken)),
            EmptyStatementSyntax empty => builder.Token(empty.SemicolonToken),
            _ => ControlFlow(statement, builder),
        };

        if (doc is null)
        {
            builder.Reject();
        }

        return doc;
    }

    /// <summary>
    /// Prints a block: the braces on their own lines (Allman) and the statements indented one level, or
    /// <c>{ }</c> when it holds nothing.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <param name="builder">Collects the block's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the trivia before its closing brace is in a position that is kept as written.</returns>
    public static Doc? Block(BlockSyntax block, NodeBuilder builder)
    {
        LeadingTrivia? closing = TriviaLines.ParseLeading(block.CloseBraceToken.LeadingTrivia, atEndOfFile: false);
        OpenBrace? open = BraceLayout.Open(block.OpenBraceToken, builder);
        if (closing is null || open is not { } brace)
        {
            builder.Reject();
            return null;
        }

        Doc close = builder.Token(block.CloseBraceToken, leadingHandled: true);
        if (block.Statements.Count == 0 && closing.Lines.Count == 0)
        {
            return Docs.Concat(brace.Doc, brace.HasComment ? Docs.HardLine : Docs.Text(" "), close);
        }

        return Docs.Concat(
            brace.Doc,
            Docs.Indent(Docs.Concat(Docs.HardLine, MemberList.Print([.. block.Statements], closing))),
            Docs.HardLine,
            close);
    }

    /// <summary>
    /// Returns whether <paramref name="block"/> prints as <c>{ }</c> on the line of its header: it has no statements, and no
    /// comment before its closing brace or after its opening brace, and (when its brace starts its own line) nothing above it.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <param name="openIsFirst">Whether the opening brace is the first token of the node being printed, so the list prints what is above it.</param>
    /// <returns><see langword="true"/> when the block is empty and plain.</returns>
    public static bool IsEmpty(BlockSyntax block, bool openIsFirst) =>
        block.Statements.Count == 0 && BraceLayout.IsEmpty(block.OpenBraceToken, block.CloseBraceToken, openIsFirst);

    private static Doc? ControlFlow(StatementSyntax statement, NodeBuilder builder) => statement switch
    {
        IfStatementSyntax @if => ControlFlowPrinter.If(@if, builder),
        ForStatementSyntax @for => ControlFlowPrinter.For(@for, builder),
        CommonForEachStatementSyntax each => ControlFlowPrinter.ForEach(each, builder),
        WhileStatementSyntax @while => ControlFlowPrinter.While(@while, builder),
        DoStatementSyntax @do => ControlFlowPrinter.Do(@do, builder),
        _ => Compound(statement, builder),
    };

    private static Doc? Compound(StatementSyntax statement, NodeBuilder builder) => statement switch
    {
        LocalFunctionStatementSyntax function => MemberPrinter.LocalFunction(function, builder),
        UsingStatementSyntax @using => CompoundStatementPrinter.Using(@using, builder),
        LockStatementSyntax @lock => CompoundStatementPrinter.Lock(@lock, builder),
        FixedStatementSyntax @fixed => CompoundStatementPrinter.Fixed(@fixed, builder),
        CheckedStatementSyntax @checked => CompoundStatementPrinter.KeywordBlock(@checked.Keyword, @checked.Block, builder),
        UnsafeStatementSyntax @unsafe => CompoundStatementPrinter.KeywordBlock(@unsafe.UnsafeKeyword, @unsafe.Block, builder),
        TryStatementSyntax @try => CompoundStatementPrinter.Try(@try, builder),
        SwitchStatementSyntax @switch => SwitchPrinter.Switch(@switch, builder),
        GotoStatementSyntax @goto => Goto(@goto, builder),
        LabeledStatementSyntax labeled => Labeled(labeled, builder),
        _ => null,
    };

    private static Doc Labeled(LabeledStatementSyntax statement, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(statement.Identifier),
            builder.Token(statement.ColonToken),
            Docs.HardLine,
            NodePrinter.PrintAlone(statement.Statement, trailingByAncestor: true));

    private static Doc Goto(GotoStatementSyntax statement, NodeBuilder builder)
    {
        var parts = new List<Doc> { builder.Token(statement.GotoKeyword) };
        if (!statement.CaseOrDefaultKeyword.IsKind(SyntaxKind.None))
        {
            parts.Add(Docs.Text(" "));
            parts.Add(builder.Token(statement.CaseOrDefaultKeyword));
        }

        if (statement.Expression is not null)
        {
            parts.Add(Docs.Text(" "));
            parts.Add(ExpressionPrinter.Print(statement.Expression, builder));
        }

        parts.Add(builder.Token(statement.SemicolonToken));
        return Docs.Concat(parts);
    }

    private static Doc Concat(Doc first, Doc second) => Docs.Concat(first, second);

    private static Doc LocalDeclaration(LocalDeclarationStatementSyntax local, NodeBuilder builder)
    {
        var prefix = new List<SyntaxToken>();
        if (!local.AwaitKeyword.IsKind(SyntaxKind.None))
        {
            prefix.Add(local.AwaitKeyword);
        }

        if (!local.UsingKeyword.IsKind(SyntaxKind.None))
        {
            prefix.Add(local.UsingKeyword);
        }

        prefix.AddRange(local.Modifiers);
        return Docs.Concat(DeclaratorPrinter.Declaration(local.Declaration, prefix, builder), builder.Token(local.SemicolonToken));
    }

    private static Doc Keyword(SyntaxToken keyword, ExpressionSyntax? value, SyntaxToken semicolon, NodeBuilder builder)
    {
        Doc head = builder.Token(keyword);
        return value is null
            ? Docs.Concat(head, builder.Token(semicolon))
            : Docs.Concat(head, Docs.Text(" "), ExpressionPrinter.Print(value, builder), builder.Token(semicolon));
    }

    private static Doc Yield(YieldStatementSyntax statement, NodeBuilder builder)
    {
        Doc head = Docs.Concat(builder.Token(statement.YieldKeyword), Docs.Text(" "), builder.Token(statement.ReturnOrBreakKeyword));
        return statement.Expression is null
            ? Docs.Concat(head, builder.Token(statement.SemicolonToken))
            : Docs.Concat(head, Docs.Text(" "), ExpressionPrinter.Print(statement.Expression, builder), builder.Token(statement.SemicolonToken));
    }
}
