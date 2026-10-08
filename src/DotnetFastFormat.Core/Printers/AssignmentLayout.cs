using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Lays out <c>target = value</c> for assignments, declarators, initializers and expression bodies (ADR 0013). A value that
/// can break inside itself (a call, a creation, a lambda, a conditional) stays on the line of the operator. A binary
/// or logical value, a single-line string and a plain member access move to the next line, indented, when the whole
/// does not fit.
/// </summary>
internal static class AssignmentLayout
{
    /// <summary>Prints the operator and the value after a target.</summary>
    /// <param name="target">What comes before the operator, already printed.</param>
    /// <param name="operatorToken">The <c>=</c>, <c>+=</c>, <c>=&gt;</c> and so on.</param>
    /// <param name="value">The value.</param>
    /// <param name="builder">Collects the tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Print(Doc target, SyntaxToken operatorToken, ExpressionSyntax value, NodeBuilder builder)
    {
        Doc head = Docs.Concat(target, Docs.Text(" "), builder.Token(operatorToken));
        Doc rhs = ExpressionPrinter.Print(value, builder, ExprContext.Assigned);
        return BreaksAfterOperator(value)
            ? Docs.Group(Docs.Concat(head, Docs.Indent(Docs.Concat(Docs.Line, rhs))))
            : Docs.Concat(head, Docs.Text(" "), rhs);
    }

    /// <summary>
    /// Prints <c>=&gt; value</c> after a lambda's parameters or a member's signature: the value stays on the arrow's line
    /// when it opens with braces or brackets, and otherwise goes on the next line, indented, when it does not fit.
    /// </summary>
    /// <param name="header">What comes before the value, ending in the arrow.</param>
    /// <param name="value">The value.</param>
    /// <param name="builder">Collects the tokens.</param>
    /// <returns>The document.</returns>
    public static Doc ArrowValue(Doc header, ExpressionSyntax value, NodeBuilder builder)
    {
        Doc printed = ExpressionPrinter.Print(value, builder, ExprContext.Assigned);
        return OpensWithBraces(value)
            ? Docs.Concat(header, Docs.Text(" "), printed)
            : Docs.Concat(header, Docs.Group(Docs.Indent(Docs.Concat(Docs.Line, printed))));
    }

    /// <summary>Returns whether a value opens with braces or brackets, so it stays on its operator's line and breaks inside itself.</summary>
    /// <param name="expression">The value.</param>
    /// <returns><see langword="true"/> for a creation with an initializer, a collection expression, an anonymous object, a switch expression or a lambda.</returns>
    public static bool OpensWithBraces(ExpressionSyntax expression) => expression switch
    {
        ObjectCreationExpressionSyntax creation => creation.Initializer is not null,
        ImplicitObjectCreationExpressionSyntax creation => creation.Initializer is not null,
        ArrayCreationExpressionSyntax creation => creation.Initializer is not null,
        ImplicitArrayCreationExpressionSyntax or AnonymousObjectCreationExpressionSyntax or CollectionExpressionSyntax
            or InitializerExpressionSyntax or SwitchExpressionSyntax or LambdaExpressionSyntax => true,
        _ => false,
    };

    private static bool BreaksAfterOperator(ExpressionSyntax value) => value switch
    {
        BinaryExpressionSyntax => true,
        ConditionalExpressionSyntax conditional => conditional.Condition is BinaryExpressionSyntax,
        LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.StringLiteralExpression)
            && literal.Token.Text.AsSpan().IndexOfAny('\r', '\n') < 0,
        InterpolatedStringExpressionSyntax => value.ToString().AsSpan().IndexOfAny('\r', '\n') < 0,
        MemberAccessExpressionSyntax member => IsPlainMemberChain(member),
        _ => false,
    };

    private static bool IsPlainMemberChain(MemberAccessExpressionSyntax member)
    {
        ExpressionSyntax current = member;
        while (current is MemberAccessExpressionSyntax access)
        {
            current = access.Expression;
        }

        return current is IdentifierNameSyntax or ThisExpressionSyntax or PredefinedTypeSyntax;
    }
}
