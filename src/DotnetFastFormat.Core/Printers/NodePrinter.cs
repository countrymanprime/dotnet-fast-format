using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Chooses the printer for a node and falls back to verbatim text where there is none (ADR 0008, ADR 0010).</summary>
internal static class NodePrinter
{
    /// <summary>Prints <paramref name="node"/>, an item of a list of siblings or the body of another statement.</summary>
    /// <param name="node">A declaration, directive or statement.</param>
    /// <param name="trailingByAncestor">Whether a node that contains this one and ends on the same token prints the comment after it (ADR 0014), so this node neither reads nor repeats it.</param>
    /// <returns>The document for the node and the trivia around it that the list prints.</returns>
    public static PrintedNode Print(SyntaxNode node, bool trailingByAncestor = false)
    {
        SyntaxToken first = node.GetFirstToken();
        SyntaxToken last = node.GetLastToken();
        bool ownsTrailing = node is not FileScopedNamespaceDeclarationSyntax && !trailingByAncestor;

        LeadingTrivia? leading = TriviaLines.ParseLeading(first.LeadingTrivia, atEndOfFile: false);
        (bool trailingClean, string? trailing) = ownsTrailing
            ? TriviaLines.ParseTrailing(last.TrailingTrivia)
            : (true, null);
        bool trailingPrinted = trailingByAncestor || (ownsTrailing && trailingClean);

        var builder = new NodeBuilder(node, leadingPrinted: leading is not null, trailingPrinted: trailingPrinted);
        Doc? doc = leading is not null && (trailingClean || !ownsTrailing) ? Format(node, builder) : null;
        if (doc is not null && builder.IsSafe)
        {
            return new PrintedNode(doc, leading, trailing);
        }

        return new PrintedNode(
            Verbatim(node, leading is not null, trailingPrinted),
            leading,
            trailingPrinted ? trailing : null);
    }

    /// <summary>Prints <paramref name="node"/> as a list of one, with the comments above and after it.</summary>
    /// <param name="node">The statement.</param>
    /// <param name="trailingByAncestor">Whether the comment after the node is printed by a node that contains it.</param>
    /// <returns>The document, with no line break before or after it.</returns>
    public static Doc PrintAlone(SyntaxNode node, bool trailingByAncestor)
    {
        PrintedNode printed = Print(node, trailingByAncestor);
        return Docs.Concat(
            TriviaPrinter.Leading(printed.Leading),
            printed.Doc,
            printed.TrailingComment is { } comment ? Docs.LineSuffix(" " + comment) : Docs.Empty);
    }

    private static Doc? Format(SyntaxNode node, NodeBuilder builder) => node switch
    {
        UsingDirectiveSyntax or ExternAliasDirectiveSyntax => builder.Tokens(node),
        BaseNamespaceDeclarationSyntax ns => NamespacePrinter.Print(ns, builder),
        TypeDeclarationSyntax type => TypePrinter.Print(type, builder),
        SwitchSectionSyntax section => SwitchPrinter.Section(section, builder),
        GlobalStatementSyntax global => StatementPrinter.Global(global, builder),
        StatementSyntax statement => StatementPrinter.Print(statement, builder),
        FieldDeclarationSyntax field => MemberPrinter.Field(field, builder),
        AccessorDeclarationSyntax accessor => MemberPrinter.Accessor(accessor, builder),
        PropertyDeclarationSyntax property => MemberPrinter.Property(property, builder),
        MethodDeclarationSyntax method => MemberPrinter.Method(method, builder),
        ConstructorDeclarationSyntax constructor => MemberPrinter.Constructor(constructor, builder),
        _ => OtherMemberPrinter.Print(node, builder),
    };

    /// <summary>Copies the node as written, leaving out the trivia the list prints.</summary>
    private static Doc Verbatim(SyntaxNode node, bool leadingPrinted, bool trailingPrinted)
    {
        string text = node.ToFullString();
        int start = leadingPrinted ? node.GetFirstToken().Span.Start - node.FullSpan.Start : 0;
        int end = trailingPrinted ? node.GetLastToken().Span.End - node.FullSpan.Start : text.Length;
        return Docs.Verbatim(text[start..end].Trim());
    }
}
