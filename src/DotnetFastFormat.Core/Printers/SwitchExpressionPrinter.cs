using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints <c>value switch { pattern =&gt; result, ... }</c> with the braces on their own lines and one arm per line (ADR 0013).</summary>
internal static class SwitchExpressionPrinter
{
    /// <summary>Prints a switch expression; the patterns are copied as written.</summary>
    /// <param name="expression">The switch expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Print(SwitchExpressionSyntax expression, NodeBuilder builder)
    {
        Doc head = Docs.Concat(
            ExpressionPrinter.Print(expression.GoverningExpression, builder),
            Docs.Text(" "),
            builder.Token(expression.SwitchKeyword));
        Doc open = builder.Token(expression.OpenBraceToken);
        Doc close = builder.Token(expression.CloseBraceToken);
        if (expression.Arms.Count == 0)
        {
            return Docs.Concat(head, Docs.Text(" "), open, Docs.Text(" "), close);
        }

        var arms = new List<Doc>();
        for (int i = 0; i < expression.Arms.Count; i++)
        {
            arms.Add(Arm(expression.Arms[i], builder));
            if (i < expression.Arms.GetSeparators().Count())
            {
                arms.Add(builder.Token(expression.Arms.GetSeparator(i)));
            }

            if (i < expression.Arms.Count - 1)
            {
                arms.Add(Docs.HardLine);
            }
        }

        return Docs.Concat(head, Docs.HardLine, open, Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Concat(arms))), Docs.HardLine, close);
    }

    private static Doc Arm(SwitchExpressionArmSyntax arm, NodeBuilder builder)
    {
        Doc pattern = builder.Verbatim(arm.Pattern);
        Doc guard = arm.WhenClause is null
            ? Docs.Empty
            : Docs.Concat(Docs.Text(" "), builder.Token(arm.WhenClause.WhenKeyword), Docs.Text(" "), ExpressionPrinter.Print(arm.WhenClause.Condition, builder));
        return AssignmentLayout.Print(Docs.Concat(pattern, guard), arm.EqualsGreaterThanToken, arm.Expression, builder);
    }
}
