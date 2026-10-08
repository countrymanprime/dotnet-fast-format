using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints an attribute list <c>[Name(arguments), Other]</c> (ADR 0013): on one line when it fits, otherwise the
/// arguments one per line like an argument list. A list with a comment or directive inside it is copied as written.
/// </summary>
internal static class AttributePrinter
{
    /// <summary>Prints one attribute list, without the line break after it.</summary>
    /// <param name="list">The attribute list.</param>
    /// <param name="builder">Collects the list's own tokens.</param>
    /// <param name="startsOwnLine">Whether the list starts a line of its own, so a comment above it can be copied with it.</param>
    /// <param name="trailingHandled">Whether the caller prints a comment after the list's last token itself.</param>
    /// <returns>The document.</returns>
    public static Doc List(AttributeListSyntax list, NodeBuilder builder, bool startsOwnLine, bool trailingHandled)
    {
        if (HasInnerTrivia(list))
        {
            return builder.Verbatim(list, startsOwnLine, trailingHandled: trailingHandled);
        }

        var parts = new List<Doc> { builder.Token(list.OpenBracketToken, trailingHandled: false) };
        if (list.Target is { } target)
        {
            parts.Add(builder.Token(target.Identifier));
            parts.Add(builder.Token(target.ColonToken));
            parts.Add(Docs.Text(" "));
        }

        var attributes = new List<Doc>();
        foreach (AttributeSyntax attribute in list.Attributes)
        {
            attributes.Add(Attribute(attribute, builder));
        }

        parts.Add(attributes.Count == 1
            ? attributes[0]
            : DelimitedListPrinter.Print(Docs.Empty, Docs.Empty, attributes, DelimitedListPrinter.Separators(list.Attributes), builder, Docs.SoftLine, before: null));
        parts.Add(builder.Token(list.CloseBracketToken, trailingHandled: trailingHandled));
        return Docs.Concat(parts);
    }

    private static Doc Attribute(AttributeSyntax attribute, NodeBuilder builder)
    {
        Doc name = builder.Tokens(attribute.Name);
        if (attribute.ArgumentList is not { } arguments)
        {
            return name;
        }

        Doc open = builder.Token(arguments.OpenParenToken);
        Doc close = builder.Token(arguments.CloseParenToken);
        if (arguments.Arguments.Count == 0)
        {
            return Docs.Concat(name, open, close);
        }

        var items = new List<Doc>();
        foreach (AttributeArgumentSyntax argument in arguments.Arguments)
        {
            items.Add(Argument(argument, builder));
        }

        var commas = new List<Doc>();
        foreach (SyntaxToken comma in arguments.Arguments.GetSeparators())
        {
            commas.Add(builder.Token(comma));
        }

        var body = new List<Doc>();
        for (int i = 0; i < items.Count; i++)
        {
            body.Add(items[i]);
            if (i < commas.Count)
            {
                body.Add(commas[i]);
                body.Add(Docs.Line);
            }
        }

        return Docs.Concat(
            name,
            Docs.Group(Docs.Concat(open, Docs.Indent(Docs.Concat(Docs.SoftLine, Docs.Concat(body))), Docs.SoftLine, close)));
    }

    private static Doc Argument(AttributeArgumentSyntax argument, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        if (argument.NameEquals is { } nameEquals)
        {
            parts.Add(builder.Tokens(nameEquals.Name));
            parts.Add(Docs.Text(" "));
            parts.Add(builder.Token(nameEquals.EqualsToken));
            parts.Add(Docs.Text(" "));
        }
        else if (argument.NameColon is { } nameColon)
        {
            parts.Add(builder.Tokens(nameColon.Name));
            parts.Add(builder.Token(nameColon.ColonToken));
            parts.Add(Docs.Text(" "));
        }

        parts.Add(ExpressionPrinter.Print(argument.Expression, builder));
        return Docs.Concat(parts);
    }

    /// <summary>Returns whether a comment or directive sits inside the list, between its first and last token.</summary>
    private static bool HasInnerTrivia(AttributeListSyntax list)
    {
        SyntaxToken first = list.GetFirstToken();
        SyntaxToken last = list.GetLastToken();
        foreach (SyntaxToken token in list.DescendantTokens())
        {
            if ((token != first && TriviaRules.HasCommentOrDirective(token.LeadingTrivia))
                || (token != last && TriviaRules.HasCommentOrDirective(token.TrailingTrivia)))
            {
                return true;
            }
        }

        return false;
    }
}
