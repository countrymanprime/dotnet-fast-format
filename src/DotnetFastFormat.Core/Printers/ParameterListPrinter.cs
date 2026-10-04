using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints a parenthesized parameter list on one line when it fits and one parameter per line otherwise.</summary>
internal static class ParameterListPrinter
{
    /// <summary>Prints <paramref name="list"/>.</summary>
    /// <param name="list">The parameter list.</param>
    /// <param name="builder">Collects the list's own tokens.</param>
    /// <returns>The document; the builder is marked unsafe when a parameter has syntax this printer leaves verbatim.</returns>
    public static Doc Print(ParameterListSyntax list, NodeBuilder builder)
    {
        Doc open = builder.Token(list.OpenParenToken);
        Doc close = builder.Token(list.CloseParenToken);
        if (list.Parameters.Count == 0)
        {
            return Docs.Concat(open, close);
        }

        var items = new List<Doc>();
        foreach (ParameterSyntax parameter in list.Parameters)
        {
            if (parameter.AttributeLists.Count > 0 || !IsSimpleDefault(parameter.Default))
            {
                builder.Reject();
            }

            items.Add(builder.Tokens(parameter));
        }

        var commas = new List<Doc>();
        foreach (SyntaxToken separator in list.Parameters.GetSeparators())
        {
            commas.Add(Docs.Concat(builder.Token(separator), Docs.Line));
        }

        var joined = new List<Doc>();
        for (int i = 0; i < items.Count; i++)
        {
            joined.Add(items[i]);
            if (i < commas.Count)
            {
                joined.Add(commas[i]);
            }
        }

        return Docs.Group(Docs.Concat(
            open,
            Docs.Indent(Docs.Concat(Docs.SoftLine, Docs.Concat(joined))),
            Docs.SoftLine,
            close));
    }

    private static bool IsSimpleDefault(EqualsValueClauseSyntax? clause) => clause is null || IsSimpleValue(clause.Value);

    private static bool IsSimpleValue(ExpressionSyntax value) => value switch
    {
        LiteralExpressionSyntax or IdentifierNameSyntax => true,
        PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.UnaryMinusExpression or (int)SyntaxKind.UnaryPlusExpression, Operand: LiteralExpressionSyntax } => true,
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } access =>
            IsSimpleValue(access.Expression) && access.Name is IdentifierNameSyntax,
        _ => false,
    };
}
