using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints argument lists (ADR 0013): on one line when they fit, otherwise one argument per line with the closing
/// parenthesis after the last argument. A last argument that is a lambda with a block body, or a creation with an
/// initializer, is opened on the call's line instead when the text before it fits.
/// </summary>
internal static class ArgumentListPrinter
{
    /// <summary>Prints <c>(a, b)</c>.</summary>
    /// <param name="list">The argument list.</param>
    /// <param name="builder">Collects the list's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Print(ArgumentListSyntax list, NodeBuilder builder) =>
        Print(list.OpenParenToken, list.Arguments, list.CloseParenToken, builder, hugLast: true);

    /// <summary>Prints <c>[a, b]</c>.</summary>
    /// <param name="list">The bracketed argument list.</param>
    /// <param name="builder">Collects the list's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Bracketed(BracketedArgumentListSyntax list, NodeBuilder builder) =>
        Print(list.OpenBracketToken, list.Arguments, list.CloseBracketToken, builder, hugLast: false);

    /// <summary>Prints arguments between two tokens.</summary>
    /// <param name="open">The opening token.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="close">The closing token.</param>
    /// <param name="builder">Collects the tokens.</param>
    /// <param name="hugLast">Whether a last argument that can be opened on the line of the call is.</param>
    /// <returns>The document.</returns>
    public static Doc Print(
        SyntaxToken open,
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        SyntaxToken close,
        NodeBuilder builder,
        bool hugLast)
    {
        Doc openDoc = builder.Token(open);
        Doc closeDoc = builder.Token(close);
        if (arguments.Count == 0)
        {
            return Docs.Concat(openDoc, closeDoc);
        }

        var items = new List<Doc>();
        LambdaParts? lastLambda = null;
        foreach (ArgumentSyntax argument in arguments)
        {
            if (hugLast && argument == arguments[^1] && IsPlainLambda(argument) && LambdaPrinter.Split((LambdaExpressionSyntax)argument.Expression, builder) is { } parts)
            {
                lastLambda = parts;
                items.Add(LambdaPrinter.Compose(parts));
                continue;
            }

            items.Add(Argument(argument, builder));
        }

        var commas = new List<Doc>();
        foreach (SyntaxToken comma in arguments.GetSeparators())
        {
            commas.Add(builder.Token(comma));
        }

        Doc Broken(bool forced) => Docs.Group(
            Docs.Concat(openDoc, Docs.Indent(Docs.Concat(Docs.SoftLine, Join(items, commas, Docs.Line))), Docs.SoftLine, closeDoc),
            forced);

        if (!hugLast || (lastLambda is null && !CanOpenOnTheCallsLine(arguments[^1].Expression)))
        {
            // An argument that holds a forced break (a block lambda inside a nested call) puts every argument on its own line.
            return Broken(forced: AnyWillBreak(items, items.Count));
        }

        if (AnyWillBreak(items, items.Count - 1))
        {
            return Broken(forced: true);
        }

        Doc Hugged(Doc last)
        {
            var parts = new List<Doc>(items) { [^1] = last };
            return Docs.Concat(openDoc, Join(parts, commas, Docs.Text(" ")), closeDoc);
        }

        // A lambda with an expression body is opened on the call's line as `x =>`, its body on the next line and the closing parenthesis below it.
        Doc opened = lastLambda is null
            ? Docs.Group(items[^1], forceBreak: true)
            : Docs.Group(Docs.Concat(lastLambda.Header, Docs.Indent(Docs.Concat(Docs.Line, lastLambda.Body)), Docs.SoftLine), forceBreak: true);
        return Docs.Conditional(Hugged(items[^1]), Hugged(opened), Broken(forced: true));
    }

    private static bool AnyWillBreak(List<Doc> items, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (items[i].WillBreak)
            {
                return true;
            }
        }

        return false;
    }

    private static Doc Join(List<Doc> items, List<Doc> commas, Doc separator)
    {
        var parts = new List<Doc>();
        for (int i = 0; i < items.Count; i++)
        {
            parts.Add(items[i]);
            if (i < commas.Count)
            {
                parts.Add(commas[i]);
                parts.Add(separator);
            }
        }

        return Docs.Concat(parts);
    }

    private static Doc Argument(ArgumentSyntax argument, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        if (argument.NameColon is { } nameColon)
        {
            parts.Add(builder.Tokens(nameColon.Name));
            parts.Add(builder.Token(nameColon.ColonToken));
            parts.Add(Docs.Text(" "));
        }

        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
        {
            parts.Add(builder.Token(argument.RefKindKeyword));
            parts.Add(Docs.Text(" "));
        }

        parts.Add(ExpressionPrinter.Print(argument.Expression, builder));
        return Docs.Concat(parts);
    }

    private static bool IsPlainLambda(ArgumentSyntax argument) =>
        argument.NameColon is null && argument.RefKindKeyword.IsKind(SyntaxKind.None) && argument.Expression is LambdaExpressionSyntax { ExpressionBody: not null };

    private static bool CanOpenOnTheCallsLine(ExpressionSyntax last) => last switch
    {
        LambdaExpressionSyntax lambda => lambda.Block is not null || (lambda.ExpressionBody is { } body && AssignmentLayout.OpensWithBraces(body)),
        AnonymousMethodExpressionSyntax method => method.Block is not null,
        ObjectCreationExpressionSyntax creation => creation.Initializer is not null,
        ImplicitObjectCreationExpressionSyntax creation => creation.Initializer is not null,
        AnonymousObjectCreationExpressionSyntax => true,
        _ => false,
    };
}
