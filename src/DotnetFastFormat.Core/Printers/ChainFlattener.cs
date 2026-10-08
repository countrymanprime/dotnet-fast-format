using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Turns <c>a.B(x)?.C[0]!</c>, a tree that nests to the left, into a root and a list of links in source order.</summary>
internal static class ChainFlattener
{
    /// <summary>Flattens a chain without recursing along its length.</summary>
    /// <param name="expression">The outermost expression of the chain.</param>
    /// <param name="links">Receives the links in source order.</param>
    /// <returns>The root: the innermost expression that is not a link, or <see langword="null"/> when the chain starts with a binding (<c>.B</c> after a <c>?</c>).</returns>
    public static ExpressionSyntax? Flatten(ExpressionSyntax expression, List<ChainLink> links)
    {
        if (expression is ConditionalAccessExpressionSyntax access)
        {
            ExpressionSyntax? root = Flatten(access.Expression, links);
            int first = links.Count;
            Flatten(access.WhenNotNull, links);
            if (first < links.Count)
            {
                links[first] = links[first] with { Question = access.OperatorToken };
            }

            return root;
        }

        var reversed = new List<ChainLink>();
        ExpressionSyntax? current = expression;
        ExpressionSyntax? start = null;
        while (current is not null)
        {
            switch (current)
            {
                case InvocationExpressionSyntax call:
                    reversed.Add(new ChainLink(ChainLinkKind.Call, default, null, call.ArgumentList));
                    current = call.Expression;
                    break;
                case MemberAccessExpressionSyntax member:
                    reversed.Add(new ChainLink(ChainLinkKind.Member, member.OperatorToken, member.Name, null));
                    current = member.Expression;
                    break;
                case ElementAccessExpressionSyntax element:
                    reversed.Add(new ChainLink(ChainLinkKind.Index, default, null, element.ArgumentList));
                    current = element.Expression;
                    break;
                case PostfixUnaryExpressionSyntax bang when bang.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                    reversed.Add(new ChainLink(ChainLinkKind.Bang, bang.OperatorToken, null, null));
                    current = bang.Operand;
                    break;
                case MemberBindingExpressionSyntax binding:
                    reversed.Add(new ChainLink(ChainLinkKind.Member, binding.OperatorToken, binding.Name, null));
                    current = null;
                    break;
                case ElementBindingExpressionSyntax binding:
                    reversed.Add(new ChainLink(ChainLinkKind.Index, default, null, binding.ArgumentList));
                    current = null;
                    break;
                default:
                    start = current;
                    current = null;
                    break;
            }
        }

        for (int i = reversed.Count - 1; i >= 0; i--)
        {
            links.Add(reversed[i]);
        }

        return start;
    }
}
