using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints lambdas and anonymous methods (ADR 0013): the parameters and <c>=&gt;</c> on one line, then a block body on
/// the lines below (Allman) or an expression body after <c>=&gt;</c>, on the next line when it does not fit.
/// </summary>
internal static class LambdaPrinter
{
    /// <summary>Prints <c>x =&gt; body</c> and <c>(x, y) =&gt; body</c>.</summary>
    /// <param name="lambda">The lambda.</param>
    /// <param name="builder">Collects the lambda's own tokens.</param>
    /// <returns>The document, or a copy of the lambda when it has attributes or a return type.</returns>
    public static Doc Print(LambdaExpressionSyntax lambda, NodeBuilder builder)
    {
        if (lambda.AttributeLists.Count > 0 || lambda is ParenthesizedLambdaExpressionSyntax { ReturnType: not null })
        {
            return builder.Verbatim(lambda);
        }

        Doc header = Header(lambda, builder);
        return Body(header, lambda.Block, lambda.ExpressionBody, builder);
    }

    /// <summary>
    /// Splits a lambda with an expression body into its header and its body, for a call that lays the lambda out in more
    /// than one way. A lambda whose body opens with braces, has attributes or a return type is not split.
    /// </summary>
    /// <param name="lambda">The lambda.</param>
    /// <param name="builder">Collects the lambda's own tokens.</param>
    /// <returns>The parts, or <see langword="null"/> when the lambda is not split and nothing was printed.</returns>
    public static LambdaParts? Split(LambdaExpressionSyntax lambda, NodeBuilder builder)
    {
        if (lambda.ExpressionBody is not { } body
            || lambda.AttributeLists.Count > 0
            || lambda is ParenthesizedLambdaExpressionSyntax { ReturnType: not null }
            || AssignmentLayout.OpensWithBraces(body))
        {
            return null;
        }

        return new LambdaParts(Header(lambda, builder), ExpressionPrinter.Print(body, builder, ExprContext.Assigned));
    }

    /// <summary>Lays the parts out in one way: the body after <c>=&gt;</c> when it fits, otherwise on the next line, indented.</summary>
    /// <param name="parts">The parts of the lambda.</param>
    /// <returns>The document.</returns>
    public static Doc Compose(LambdaParts parts) =>
        Docs.Concat(parts.Header, Docs.Group(Docs.Indent(Docs.Concat(Docs.Line, parts.Body))));

    /// <summary>Prints <c>delegate (int x) { ... }</c>.</summary>
    /// <param name="method">The anonymous method.</param>
    /// <param name="builder">Collects the method's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Anonymous(AnonymousMethodExpressionSyntax method, NodeBuilder builder)
    {
        Doc header = Docs.Concat(
            Modifiers(method.Modifiers, builder),
            builder.Token(method.DelegateKeyword),
            method.ParameterList is null ? Docs.Empty : Docs.Concat(Docs.Text(" "), ParameterListPrinter.Print(method.ParameterList, builder)));
        return Body(header, method.Block, method.ExpressionBody, builder);
    }

    private static Doc Header(LambdaExpressionSyntax lambda, NodeBuilder builder)
    {
        Doc parameters = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => builder.Tokens(simple.Parameter),
            ParenthesizedLambdaExpressionSyntax parenthesized => ParameterListPrinter.Print(parenthesized.ParameterList, builder),
            _ => throw new InvalidOperationException("Unknown lambda kind."),
        };
        return Docs.Concat(Modifiers(lambda.Modifiers, builder), parameters, Docs.Text(" "), builder.Token(lambda.ArrowToken));
    }

    private static Doc Modifiers(SyntaxTokenList modifiers, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        foreach (SyntaxToken modifier in modifiers)
        {
            parts.Add(builder.Token(modifier));
            parts.Add(Docs.Text(" "));
        }

        return Docs.Concat(parts);
    }

    private static Doc Body(Doc header, BlockSyntax? block, ExpressionSyntax? expression, NodeBuilder builder)
    {
        if (block is not null)
        {
            Doc? printed = StatementPrinter.Block(block, builder);
            if (printed is null)
            {
                return header;
            }

            return StatementPrinter.IsEmpty(block, openIsFirst: false)
                ? Docs.Concat(header, Docs.Text(" "), printed)
                : Docs.Concat(header, Docs.HardLine, printed);
        }

        return AssignmentLayout.ArrowValue(header, expression!, builder);
    }
}
