using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints object, array, collection and anonymous-object creation, initializers, tuples and <c>with</c> (ADR 0013). The
/// braces of a creation's initializer go on their own lines when the creation does not fit on one line.
/// </summary>
internal static class CreationPrinter
{
    /// <summary>Prints <c>new T(args) { ... }</c>.</summary>
    /// <param name="creation">The creation.</param>
    /// <param name="builder">Collects the creation's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Object(ObjectCreationExpressionSyntax creation, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(creation.NewKeyword),
            Docs.Text(" "),
            builder.Tokens(creation.Type),
            creation.ArgumentList is null ? Docs.Empty : ArgumentListPrinter.Print(creation.ArgumentList, builder),
            Initializer(creation.Initializer, builder));

    /// <summary>Prints <c>new(args) { ... }</c>.</summary>
    /// <param name="creation">The creation.</param>
    /// <param name="builder">Collects the creation's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc ImplicitObject(ImplicitObjectCreationExpressionSyntax creation, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(creation.NewKeyword),
            ArgumentListPrinter.Print(creation.ArgumentList, builder),
            Initializer(creation.Initializer, builder));

    /// <summary>Prints <c>new T[n] { ... }</c>.</summary>
    /// <param name="creation">The creation.</param>
    /// <param name="builder">Collects the creation's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Array(ArrayCreationExpressionSyntax creation, NodeBuilder builder)
    {
        var parts = new List<Doc>
        {
            builder.Token(creation.NewKeyword),
            Docs.Text(" "),
            builder.Tokens(creation.Type.ElementType),
        };
        foreach (ArrayRankSpecifierSyntax rank in creation.Type.RankSpecifiers)
        {
            parts.Add(builder.Token(rank.OpenBracketToken));
            for (int i = 0; i < rank.Sizes.Count; i++)
            {
                parts.Add(rank.Sizes[i] is OmittedArraySizeExpressionSyntax ? Docs.Empty : ExpressionPrinter.Print(rank.Sizes[i], builder));
                if (i < rank.Sizes.Count - 1)
                {
                    parts.Add(builder.Token(rank.Sizes.GetSeparator(i)));
                    parts.Add(Docs.Text(" "));
                }
            }

            parts.Add(builder.Token(rank.CloseBracketToken));
        }

        parts.Add(Initializer(creation.Initializer, builder));
        return Docs.Concat(parts);
    }

    /// <summary>Prints <c>new[] { ... }</c>.</summary>
    /// <param name="creation">The creation.</param>
    /// <param name="builder">Collects the creation's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc ImplicitArray(ImplicitArrayCreationExpressionSyntax creation, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(creation.NewKeyword),
            builder.Token(creation.OpenBracketToken),
            Docs.Concat(creation.Commas.Select(comma => builder.Token(comma))),
            builder.Token(creation.CloseBracketToken),
            Initializer(creation.Initializer, builder));

    /// <summary>Prints <c>new { A = 1, B }</c>.</summary>
    /// <param name="creation">The creation.</param>
    /// <param name="builder">Collects the creation's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Anonymous(AnonymousObjectCreationExpressionSyntax creation, NodeBuilder builder)
    {
        Doc open = builder.Token(creation.OpenBraceToken);
        Doc close = builder.Token(creation.CloseBraceToken);
        Doc keyword = builder.Token(creation.NewKeyword);
        if (creation.Initializers.Count == 0)
        {
            return Docs.Concat(keyword, Docs.Text(" "), open, Docs.Text(" "), close);
        }

        var items = new List<Doc>();
        foreach (AnonymousObjectMemberDeclaratorSyntax member in creation.Initializers)
        {
            items.Add(member.NameEquals is null
                ? ExpressionPrinter.Print(member.Expression, builder)
                : Docs.Concat(builder.Tokens(member.NameEquals.Name), Docs.Text(" "), builder.Token(member.NameEquals.EqualsToken), Docs.Text(" "), ExpressionPrinter.Print(member.Expression, builder)));
        }

        return DelimitedListPrinter.Print(open, close, items, DelimitedListPrinter.Separators(creation.Initializers), builder, Docs.Line, Docs.Concat(keyword, Docs.Line));
    }

    /// <summary>Prints <c>[a, ..b]</c>.</summary>
    /// <param name="collection">The collection expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document, or a copy of the expression when it has an element this printer does not know.</returns>
    public static Doc Collection(CollectionExpressionSyntax collection, NodeBuilder builder)
    {
        var items = new List<Doc>();
        foreach (CollectionElementSyntax element in collection.Elements)
        {
            switch (element)
            {
                case ExpressionElementSyntax expression:
                    items.Add(ExpressionPrinter.Print(expression.Expression, builder));
                    break;
                case SpreadElementSyntax spread:
                    items.Add(Docs.Concat(builder.Token(spread.OperatorToken), ExpressionPrinter.Print(spread.Expression, builder)));
                    break;
                default:
                    return builder.Verbatim(collection);
            }
        }

        Doc open = builder.Token(collection.OpenBracketToken);
        Doc close = builder.Token(collection.CloseBracketToken);
        return items.Count == 0
            ? Docs.Concat(open, close)
            : DelimitedListPrinter.Print(open, close, items, DelimitedListPrinter.Separators(collection.Elements), builder, Docs.SoftLine, before: null);
    }

    /// <summary>Prints an initializer that is not the tail of a creation: <c>{ 1, 2 }</c> as a value, or a nested element initializer.</summary>
    /// <param name="initializer">The initializer.</param>
    /// <param name="builder">Collects the initializer's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Braces(InitializerExpressionSyntax initializer, NodeBuilder builder) =>
        Initializer(initializer, builder, braceOnOwnLine: false);

    /// <summary>Prints <c>x with { A = 1 }</c>.</summary>
    /// <param name="with">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc With(WithExpressionSyntax with, NodeBuilder builder) =>
        Docs.Concat(
            ExpressionPrinter.Print(with.Expression, builder),
            Docs.Text(" "),
            builder.Token(with.WithKeyword),
            Initializer(with.Initializer, builder));

    /// <summary>Prints a tuple expression <c>(a, b)</c>.</summary>
    /// <param name="tuple">The tuple.</param>
    /// <param name="builder">Collects the tuple's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Tuple(TupleExpressionSyntax tuple, NodeBuilder builder) =>
        ArgumentListPrinter.Print(tuple.OpenParenToken, tuple.Arguments, tuple.CloseParenToken, builder, hugLast: false);

    private static Doc Initializer(InitializerExpressionSyntax? initializer, NodeBuilder builder, bool braceOnOwnLine = true)
    {
        if (initializer is null)
        {
            return Docs.Empty;
        }

        Doc open = builder.Token(initializer.OpenBraceToken);
        Doc close = builder.Token(initializer.CloseBraceToken);
        if (initializer.Expressions.Count == 0)
        {
            return Docs.Concat(braceOnOwnLine ? Docs.Text(" ") : Docs.Empty, open, Docs.Text(" "), close);
        }

        var items = new List<Doc>();
        foreach (ExpressionSyntax item in initializer.Expressions)
        {
            items.Add(ExpressionPrinter.Print(item, builder));
        }

        return DelimitedListPrinter.Print(
            open,
            close,
            items,
            DelimitedListPrinter.Separators(initializer.Expressions),
            builder,
            Docs.Line,
            braceOnOwnLine ? Docs.Line : null);
    }
}
