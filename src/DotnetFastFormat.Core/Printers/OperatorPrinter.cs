using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints expressions built from operators and keywords: binary and logical, conditional, unary, cast, parenthesized,
/// <c>await</c>, <c>typeof</c> and friends (ADR 0013).
/// </summary>
internal static class OperatorPrinter
{
    /// <summary>
    /// Prints a binary expression. A chain of one operator is one group: when it breaks, every operator starts a
    /// continuation line, indented one level (not in the parentheses of a header, whose group already indents).
    /// </summary>
    /// <param name="binary">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <param name="context">Where the expression sits.</param>
    /// <returns>The document.</returns>
    public static Doc Binary(BinaryExpressionSyntax binary, NodeBuilder builder, ExprContext context)
    {
        if (binary.IsKind(SyntaxKind.IsExpression) || binary.IsKind(SyntaxKind.AsExpression))
        {
            return Docs.Concat(
                ExpressionPrinter.Print(binary.Left, builder),
                Docs.Text(" "),
                builder.Token(binary.OperatorToken),
                Docs.Text(" "),
                builder.Tokens(binary.Right));
        }

        var operands = new List<ExpressionSyntax>();
        var operators = new List<SyntaxToken>();
        Flatten(binary, operands, operators);
        Doc first = ExpressionPrinter.Print(operands[0], builder);
        var rest = new List<Doc>();
        for (int i = 0; i < operators.Count; i++)
        {
            rest.Add(Docs.Concat(Docs.Line, builder.Token(operators[i]), Docs.Text(" "), ExpressionPrinter.Print(operands[i + 1], builder)));
        }

        return context switch
        {
            ExprContext.Condition => Docs.Concat(first, Docs.Concat(rest)),
            ExprContext.Assigned => Docs.Group(Docs.Concat(first, Docs.Concat(rest))),
            _ => Docs.Group(Docs.Concat(first, Docs.Indent(Docs.Concat(rest)))),
        };
    }

    /// <summary>Prints <c>condition ? a : b</c>: one group; when it breaks, <c>?</c> and <c>:</c> start lines indented one level.</summary>
    /// <param name="conditional">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Conditional(ConditionalExpressionSyntax conditional, NodeBuilder builder) =>
        Docs.Group(Docs.Concat(
            ExpressionPrinter.Print(conditional.Condition, builder),
            Docs.Indent(Docs.Concat(
                Docs.Line,
                builder.Token(conditional.QuestionToken),
                Docs.Text(" "),
                ExpressionPrinter.Print(conditional.WhenTrue, builder),
                Docs.Line,
                builder.Token(conditional.ColonToken),
                Docs.Text(" "),
                ExpressionPrinter.Print(conditional.WhenFalse, builder)))));

    /// <summary>Prints a prefix unary expression such as <c>!a</c>, <c>-a</c> or <c>++a</c>.</summary>
    /// <param name="unary">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Prefix(PrefixUnaryExpressionSyntax unary, NodeBuilder builder)
    {
        Doc op = builder.Token(unary.OperatorToken);
        Doc operand = ExpressionPrinter.Print(unary.Operand, builder);

        // Two signs that touch would read as one token: - -a is not --a.
        bool apart = unary.Operand is PrefixUnaryExpressionSyntax inner
            && unary.OperatorToken.Text[^1] is '+' or '-'
            && inner.OperatorToken.Text[0] == unary.OperatorToken.Text[^1];
        return Docs.Concat(op, apart ? Docs.Text(" ") : Docs.Empty, operand);
    }

    /// <summary>Prints <c>a++</c>.</summary>
    /// <param name="unary">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Postfix(PostfixUnaryExpressionSyntax unary, NodeBuilder builder) =>
        Docs.Concat(ExpressionPrinter.Print(unary.Operand, builder), builder.Token(unary.OperatorToken));

    /// <summary>Prints <c>(T)value</c>.</summary>
    /// <param name="cast">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Cast(CastExpressionSyntax cast, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(cast.OpenParenToken),
            builder.Tokens(cast.Type),
            builder.Token(cast.CloseParenToken),
            ExpressionPrinter.Print(cast.Expression, builder));

    /// <summary>Prints <c>(value)</c>.</summary>
    /// <param name="parenthesized">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Parenthesized(ParenthesizedExpressionSyntax parenthesized, NodeBuilder builder) =>
        Docs.Concat(
            builder.Token(parenthesized.OpenParenToken),
            ExpressionPrinter.Print(parenthesized.Expression, builder),
            builder.Token(parenthesized.CloseParenToken));

    /// <summary>Prints <c>keyword operand</c> for <c>await</c>, <c>throw</c> and <c>ref</c>.</summary>
    /// <param name="keyword">The keyword token.</param>
    /// <param name="operand">The operand.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <param name="context">Where the whole expression sits, which the operand shares for <c>await</c>.</param>
    /// <returns>The document.</returns>
    public static Doc Keyword(SyntaxToken keyword, ExpressionSyntax operand, NodeBuilder builder, ExprContext context) =>
        Docs.Concat(builder.Token(keyword), Docs.Text(" "), ExpressionPrinter.Print(operand, builder, context));

    /// <summary>Prints <c>typeof(T)</c>, <c>sizeof(T)</c> or <c>default(T)</c>.</summary>
    /// <param name="keyword">The keyword token.</param>
    /// <param name="open">The opening parenthesis.</param>
    /// <param name="type">The type.</param>
    /// <param name="close">The closing parenthesis.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc TypeOperator(SyntaxToken keyword, SyntaxToken open, TypeSyntax type, SyntaxToken close, NodeBuilder builder) =>
        Docs.Concat(builder.Token(keyword), builder.Token(open), builder.Tokens(type), builder.Token(close));

    /// <summary>Prints <c>a..b</c>, <c>a..</c>, <c>..b</c>.</summary>
    /// <param name="range">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Range(RangeExpressionSyntax range, NodeBuilder builder) =>
        Docs.Concat(
            range.LeftOperand is null ? Docs.Empty : ExpressionPrinter.Print(range.LeftOperand, builder),
            builder.Token(range.OperatorToken),
            range.RightOperand is null ? Docs.Empty : ExpressionPrinter.Print(range.RightOperand, builder));

    /// <summary>Prints <c>expression is pattern</c>; the pattern is copied as written.</summary>
    /// <param name="isPattern">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc IsPattern(IsPatternExpressionSyntax isPattern, NodeBuilder builder) =>
        Docs.Concat(
            ExpressionPrinter.Print(isPattern.Expression, builder),
            Docs.Text(" "),
            builder.Token(isPattern.IsKeyword),
            Docs.Text(" "),
            builder.Verbatim(isPattern.Pattern));

    /// <summary>Prints a declaration expression such as <c>var x</c> or <c>int y</c>; <c>var (a, b)</c> is copied as written.</summary>
    /// <param name="declaration">The expression.</param>
    /// <param name="builder">Collects the expression's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Declaration(DeclarationExpressionSyntax declaration, NodeBuilder builder) =>
        declaration.Designation is SingleVariableDesignationSyntax or DiscardDesignationSyntax
            ? Docs.Concat(builder.Tokens(declaration.Type), Docs.Text(" "), builder.Tokens(declaration.Designation))
            : builder.Verbatim(declaration);

    /// <summary>
    /// Collects the operands of a chain of one operator: the left spine for left-associative operators, the right spine
    /// for <c>??</c>.
    /// </summary>
    private static void Flatten(BinaryExpressionSyntax binary, List<ExpressionSyntax> operands, List<SyntaxToken> operators)
    {
        BinaryExpressionSyntax current = binary;
        if (binary.IsKind(SyntaxKind.CoalesceExpression))
        {
            while (true)
            {
                operands.Add(current.Left);
                operators.Add(current.OperatorToken);
                if (current.Right is BinaryExpressionSyntax next && next.IsKind(SyntaxKind.CoalesceExpression))
                {
                    current = next;
                    continue;
                }

                operands.Add(current.Right);
                return;
            }
        }

        var reversedOperands = new List<ExpressionSyntax>();
        var reversedOperators = new List<SyntaxToken>();
        while (true)
        {
            reversedOperands.Add(current.Right);
            reversedOperators.Add(current.OperatorToken);
            if (current.Left is BinaryExpressionSyntax left && SameChain(binary, left))
            {
                current = left;
                continue;
            }

            reversedOperands.Add(current.Left);
            break;
        }

        for (int i = reversedOperands.Count - 1; i >= 0; i--)
        {
            operands.Add(reversedOperands[i]);
        }

        for (int i = reversedOperators.Count - 1; i >= 0; i--)
        {
            operators.Add(reversedOperators[i]);
        }
    }

    private static bool SameChain(BinaryExpressionSyntax outer, BinaryExpressionSyntax inner)
    {
        SyntaxKind a = outer.Kind();
        SyntaxKind b = inner.Kind();
        return a == b
            || (IsAdditive(a) && IsAdditive(b))
            || (IsMultiplicative(a) && IsMultiplicative(b));
    }

    private static bool IsAdditive(SyntaxKind kind) => kind is SyntaxKind.AddExpression or SyntaxKind.SubtractExpression;

    private static bool IsMultiplicative(SyntaxKind kind) => kind is SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression;
}
