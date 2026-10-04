using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints siblings one after another with the blank lines of ADR 0007.</summary>
internal static class MemberList
{
    /// <summary>Prints <paramref name="items"/>, each on its own line, separated as the style requires.</summary>
    /// <param name="items">The sibling nodes, in source order.</param>
    /// <returns>The document, with no line break before the first item or after the last.</returns>
    public static Doc Print(IReadOnlyList<SyntaxNode> items)
    {
        var parts = new List<Doc>();
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                parts.Add(Docs.HardLine);
                if (NeedsBlankLine(items[i - 1], items[i]))
                {
                    parts.Add(Docs.HardLine);
                }
            }

            parts.Add(NodePrinter.Print(items[i]));
        }

        return Docs.Concat(parts);
    }

    private static bool NeedsBlankLine(SyntaxNode previous, SyntaxNode next)
    {
        MemberKind kind = KindOf(previous);
        return !(kind != MemberKind.Other && kind == KindOf(next) && TriviaRules.BlankLinesBefore(next) == 0);
    }

    private static MemberKind KindOf(SyntaxNode node) => node switch
    {
        UsingDirectiveSyntax or ExternAliasDirectiveSyntax => MemberKind.Using,
        FieldDeclarationSyntax => MemberKind.Field,
        PropertyDeclarationSyntax property when IsAutoProperty(property) => MemberKind.AutoProperty,
        MethodDeclarationSyntax { Body: null, ExpressionBody: null } => MemberKind.Signature,
        EventFieldDeclarationSyntax => MemberKind.Event,
        GlobalStatementSyntax => MemberKind.Statement,
        _ => MemberKind.Other,
    };

    private static bool IsAutoProperty(PropertyDeclarationSyntax property) =>
        property.ExpressionBody is null
        && property.AccessorList is { } accessors
        && accessors.Accessors.All(a => a.Body is null && a.ExpressionBody is null);
}
