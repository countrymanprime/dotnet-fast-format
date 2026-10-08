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
    public static Doc Print(ParameterListSyntax list, NodeBuilder builder) =>
        Print(list.OpenParenToken, list.Parameters, list.CloseParenToken, builder);

    /// <summary>Prints <c>[int a, int b]</c> of an indexer.</summary>
    /// <param name="list">The bracketed parameter list.</param>
    /// <param name="builder">Collects the list's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Bracketed(BracketedParameterListSyntax list, NodeBuilder builder) =>
        Print(list.OpenBracketToken, list.Parameters, list.CloseBracketToken, builder);

    private static Doc Print(SyntaxToken openToken, SeparatedSyntaxList<ParameterSyntax> parameters, SyntaxToken closeToken, NodeBuilder builder)
    {
        Doc open = builder.Token(openToken);
        Doc close = builder.Token(closeToken);
        if (parameters.Count == 0)
        {
            return Docs.Concat(open, close);
        }

        var items = new List<Doc>();
        foreach (ParameterSyntax parameter in parameters)
        {
            items.Add(Parameter(parameter, builder));
        }

        var commas = new List<Doc>();
        foreach (SyntaxToken separator in parameters.GetSeparators())
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

    private static Doc Parameter(ParameterSyntax parameter, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        foreach (AttributeListSyntax attributes in parameter.AttributeLists)
        {
            parts.Add(AttributePrinter.List(attributes, builder, startsOwnLine: false, trailingHandled: false));
            parts.Add(Docs.Text(" "));
        }

        var tokens = new List<SyntaxToken>(parameter.Modifiers);
        if (parameter.Type is not null)
        {
            tokens.AddRange(parameter.Type.DescendantTokens());
        }

        tokens.Add(parameter.Identifier);
        parts.Add(builder.Tokens(tokens));
        if (parameter.Default is { } clause)
        {
            parts.Add(Docs.Text(" "));
            parts.Add(builder.Token(clause.EqualsToken));
            parts.Add(Docs.Text(" "));
            parts.Add(ExpressionPrinter.Print(clause.Value, builder));
        }

        return Docs.Concat(parts);
    }
}
