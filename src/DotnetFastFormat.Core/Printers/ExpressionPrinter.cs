using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints expressions (ADR 0013). An expression kind with no printer is copied as written and nothing larger (ADR 0014).</summary>
internal static class ExpressionPrinter
{
    /// <summary>Prints <paramref name="expression"/>.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="builder">Collects the tokens of the node that contains the expression.</param>
    /// <param name="context">Where the expression sits.</param>
    /// <returns>The document.</returns>
    public static Doc Print(ExpressionSyntax expression, NodeBuilder builder, ExprContext context = ExprContext.Default) =>
        expression switch
        {
            LiteralExpressionSyntax literal => builder.Literal(literal.Token),
            InterpolatedStringExpressionSyntax => builder.Verbatim(expression, forcesBreak: false),
            IdentifierNameSyntax or GenericNameSyntax or PredefinedTypeSyntax or ThisExpressionSyntax or BaseExpressionSyntax =>
                builder.Tokens(expression),
            InvocationExpressionSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or ConditionalAccessExpressionSyntax =>
                ChainPrinter.Print(expression, builder, context),
            PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression) =>
                ChainPrinter.Print(expression, builder, context),
            AssignmentExpressionSyntax assignment =>
                AssignmentLayout.Print(Print(assignment.Left, builder), assignment.OperatorToken, assignment.Right, builder),
            _ => PrintOperator(expression, builder, context),
        };

    private static Doc PrintOperator(ExpressionSyntax expression, NodeBuilder builder, ExprContext context) =>
        expression switch
        {
            BinaryExpressionSyntax binary => OperatorPrinter.Binary(binary, builder, context),
            ConditionalExpressionSyntax conditional => OperatorPrinter.Conditional(conditional, builder),
            PrefixUnaryExpressionSyntax prefix => OperatorPrinter.Prefix(prefix, builder),
            PostfixUnaryExpressionSyntax postfix => OperatorPrinter.Postfix(postfix, builder),
            CastExpressionSyntax cast => OperatorPrinter.Cast(cast, builder),
            ParenthesizedExpressionSyntax parenthesized => OperatorPrinter.Parenthesized(parenthesized, builder),
            AwaitExpressionSyntax await => OperatorPrinter.Keyword(await.AwaitKeyword, await.Expression, builder, context),
            ThrowExpressionSyntax @throw => OperatorPrinter.Keyword(@throw.ThrowKeyword, @throw.Expression, builder, ExprContext.Default),
            RefExpressionSyntax @ref => OperatorPrinter.Keyword(@ref.RefKeyword, @ref.Expression, builder, ExprContext.Default),
            RangeExpressionSyntax range => OperatorPrinter.Range(range, builder),
            IsPatternExpressionSyntax isPattern => OperatorPrinter.IsPattern(isPattern, builder),
            DeclarationExpressionSyntax declaration => OperatorPrinter.Declaration(declaration, builder),
            _ => PrintKeywordForm(expression, builder),
        };

    private static Doc PrintKeywordForm(ExpressionSyntax expression, NodeBuilder builder) =>
        expression switch
        {
            TypeOfExpressionSyntax typeOf =>
                OperatorPrinter.TypeOperator(typeOf.Keyword, typeOf.OpenParenToken, typeOf.Type, typeOf.CloseParenToken, builder),
            SizeOfExpressionSyntax sizeOf =>
                OperatorPrinter.TypeOperator(sizeOf.Keyword, sizeOf.OpenParenToken, sizeOf.Type, sizeOf.CloseParenToken, builder),
            DefaultExpressionSyntax @default =>
                OperatorPrinter.TypeOperator(@default.Keyword, @default.OpenParenToken, @default.Type, @default.CloseParenToken, builder),
            SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax => LambdaPrinter.Print((LambdaExpressionSyntax)expression, builder),
            AnonymousMethodExpressionSyntax method => LambdaPrinter.Anonymous(method, builder),
            SwitchExpressionSyntax switchExpression => SwitchExpressionPrinter.Print(switchExpression, builder),
            _ => PrintCreation(expression, builder),
        };

    private static Doc PrintCreation(ExpressionSyntax expression, NodeBuilder builder) =>
        expression switch
        {
            ObjectCreationExpressionSyntax creation => CreationPrinter.Object(creation, builder),
            ImplicitObjectCreationExpressionSyntax creation => CreationPrinter.ImplicitObject(creation, builder),
            ArrayCreationExpressionSyntax creation => CreationPrinter.Array(creation, builder),
            ImplicitArrayCreationExpressionSyntax creation => CreationPrinter.ImplicitArray(creation, builder),
            AnonymousObjectCreationExpressionSyntax creation => CreationPrinter.Anonymous(creation, builder),
            CollectionExpressionSyntax collection => CreationPrinter.Collection(collection, builder),
            InitializerExpressionSyntax initializer => CreationPrinter.Braces(initializer, builder),
            WithExpressionSyntax with => CreationPrinter.With(with, builder),
            TupleExpressionSyntax tuple => CreationPrinter.Tuple(tuple, builder),
            _ => builder.Verbatim(expression),
        };
}
