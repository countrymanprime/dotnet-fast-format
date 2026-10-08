using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Chooses the printer for a node and falls back to verbatim text where there is none (ADR 0008, ADR 0010).</summary>
internal static class NodePrinter
{
    /// <summary>Prints <paramref name="node"/>, an item of a list of siblings.</summary>
    /// <param name="node">A declaration, directive or statement.</param>
    /// <returns>The document for the node and the trivia around it that the list prints.</returns>
    public static PrintedNode Print(SyntaxNode node)
    {
        SyntaxToken first = node.GetFirstToken();
        SyntaxToken last = node.GetLastToken();
        bool ownsTrailing = node is not FileScopedNamespaceDeclarationSyntax;

        LeadingTrivia? leading = TriviaLines.ParseLeading(first.LeadingTrivia, atEndOfFile: false);
        (bool trailingClean, string? trailing) = ownsTrailing
            ? TriviaLines.ParseTrailing(last.TrailingTrivia)
            : (true, null);
        bool trailingPrinted = ownsTrailing && trailingClean;

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

    private static Doc? Format(SyntaxNode node, NodeBuilder builder) => node switch
    {
        UsingDirectiveSyntax or ExternAliasDirectiveSyntax => builder.Tokens(node),
        BaseNamespaceDeclarationSyntax ns => NamespacePrinter.Print(ns, builder),
        TypeDeclarationSyntax type => TypePrinter.Print(type, builder),
        FieldDeclarationSyntax field => MemberPrinter.Field(field, builder),
        PropertyDeclarationSyntax property => MemberPrinter.Property(property, builder),
        MethodDeclarationSyntax method => MemberPrinter.Method(method, builder),
        ConstructorDeclarationSyntax constructor => MemberPrinter.Constructor(constructor, builder),
        _ => null,
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
