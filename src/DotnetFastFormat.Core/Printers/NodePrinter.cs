using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Chooses the printer for a node and falls back to verbatim text where there is none (ADR 0008).</summary>
internal static class NodePrinter
{
    /// <summary>Prints <paramref name="node"/>, a declaration or directive.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The document for the node, with no surrounding line breaks.</returns>
    public static Doc Print(SyntaxNode node)
    {
        var builder = new NodeBuilder();
        Doc? doc = node switch
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

        return doc is not null && builder.IsSafe ? doc : VerbatimText.Of(node);
    }
}
