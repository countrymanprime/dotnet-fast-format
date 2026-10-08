using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints fields, properties, accessors, methods, constructors and local functions. Attribute lists are copied as
/// written, each on its own line above the member.
/// </summary>
internal static class MemberPrinter
{
    /// <summary>
    /// Prints the attribute lists of a member: each list on its own line. Own-line comments and
    /// directives between the lists and before the token that follows them (an <c>#if</c> around an attribute) are kept on lines
    /// of their own, and a comment after a list stays after it.
    /// </summary>
    /// <param name="lists">The attribute lists.</param>
    /// <param name="builder">Collects the member's own tokens.</param>
    /// <returns>The document, empty when there are no lists; each list is followed by a line break.</returns>
    public static Doc Attributes(SyntaxList<AttributeListSyntax> lists, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        for (int i = 0; i < lists.Count; i++)
        {
            if (i > 0)
            {
                parts.Add(LinesAbove(lists[i].GetFirstToken(), builder));
            }

            (bool clean, string? comment) = TriviaLines.ParseTrailing(lists[i].GetLastToken().TrailingTrivia);
            if (!clean)
            {
                builder.Reject();
            }

            parts.Add(AttributePrinter.List(lists[i], builder, startsOwnLine: true, trailingHandled: clean));
            parts.Add(comment is null ? Docs.Empty : Docs.LineSuffix(" " + comment));
            parts.Add(Docs.HardLine);
        }

        if (lists.Count > 0)
        {
            parts.Add(LinesAbove(lists[^1].GetLastToken().GetNextToken(), builder));
        }

        return Docs.Concat(parts);
    }

    /// <summary>Prints a field.</summary>
    /// <param name="field">The field declaration.</param>
    /// <param name="builder">Collects the field's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Field(FieldDeclarationSyntax field, NodeBuilder builder) =>
        Docs.Concat(
            Attributes(field.AttributeLists, builder),
            DeclaratorPrinter.Declaration(field.Declaration, field.Modifiers, builder),
            builder.Token(field.SemicolonToken));

    /// <summary>Prints a property: an auto-property on one line, accessors with bodies one per line, or an expression body.</summary>
    /// <param name="property">The property declaration.</param>
    /// <param name="builder">Collects the property's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the property is kept as written.</returns>
    public static Doc? Property(PropertyDeclarationSyntax property, NodeBuilder builder)
    {
        var tokens = new List<SyntaxToken>(property.Modifiers);
        tokens.AddRange(property.Type.DescendantTokens());
        if (property.ExplicitInterfaceSpecifier is not null)
        {
            tokens.AddRange(property.ExplicitInterfaceSpecifier.DescendantTokens());
        }

        tokens.Add(property.Identifier);
        Doc head = Docs.Concat(Attributes(property.AttributeLists, builder), builder.Tokens(tokens));
        return Accessors(head, property.AccessorList, property.ExpressionBody, property.SemicolonToken, builder, property.Initializer);
    }

    /// <summary>
    /// Prints what follows the name of a property, indexer or event: <c>{ get; set; }</c> on one line, accessors with bodies one
    /// per line, or an expression body; then an initializer.
    /// </summary>
    /// <param name="head">The attributes, modifiers, type and name.</param>
    /// <param name="accessors">The accessor list, or <see langword="null"/>.</param>
    /// <param name="expressionBody">The expression body, or <see langword="null"/>.</param>
    /// <param name="semicolon">The semicolon token that ends an expression body or an initializer.</param>
    /// <param name="builder">Collects the member's own tokens.</param>
    /// <param name="initializer">The value of an auto-property, or <see langword="null"/>.</param>
    /// <returns>The document, or <see langword="null"/> when the member is kept as written.</returns>
    public static Doc? Accessors(
        Doc head,
        AccessorListSyntax? accessors,
        ArrowExpressionClauseSyntax? expressionBody,
        SyntaxToken semicolon,
        NodeBuilder builder,
        EqualsValueClauseSyntax? initializer = null)
    {
        if (expressionBody is not null)
        {
            return Docs.Concat(head, ArrowBody(expressionBody, builder), builder.Token(semicolon));
        }

        Doc? list = IsInlineAccessorList(accessors!)
            ? InlineAccessors(accessors!, builder)
            : ExpandedAccessors(accessors!, builder);
        if (list is null)
        {
            return null;
        }

        Doc value = initializer is null
            ? Docs.Empty
            : Docs.Concat(AssignmentLayout.Print(Docs.Empty, initializer.EqualsToken, initializer.Value, builder), builder.Token(semicolon));
        return Docs.Concat(head, list, value);
    }

    /// <summary>Prints one accessor of a property.</summary>
    /// <param name="accessor">The accessor.</param>
    /// <param name="builder">Collects the accessor's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Accessor(AccessorDeclarationSyntax accessor, NodeBuilder builder)
    {
        Doc head = Docs.Concat(Attributes(accessor.AttributeLists, builder), builder.Tokens([.. accessor.Modifiers, accessor.Keyword]));
        if (accessor.Body is { } block)
        {
            Doc printed = StatementPrinter.Block(block, builder) ?? Docs.Empty;
            return Docs.Concat(head, StatementPrinter.IsEmpty(block, openIsFirst: false) ? Docs.Text(" ") : Docs.HardLine, printed);
        }

        return accessor.ExpressionBody is { } expression
            ? Docs.Concat(head, ArrowBody(expression, builder), builder.Token(accessor.SemicolonToken))
            : Docs.Concat(head, builder.Token(accessor.SemicolonToken));
    }

    /// <summary>Prints a method signature with its parameters wrapped to the width, and its body.</summary>
    /// <param name="method">The method declaration.</param>
    /// <param name="builder">Collects the method's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Method(MethodDeclarationSyntax method, NodeBuilder builder) =>
        Callable(
            new CallableParts(method.AttributeLists, method.Modifiers, method.ReturnType, method.ExplicitInterfaceSpecifier, method.Identifier),
            method.TypeParameterList,
            method.ParameterList,
            method.ConstraintClauses,
            (method.Body, method.ExpressionBody, method.SemicolonToken),
            builder);

    /// <summary>Prints a local function like a method.</summary>
    /// <param name="function">The local function statement.</param>
    /// <param name="builder">Collects the function's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc LocalFunction(LocalFunctionStatementSyntax function, NodeBuilder builder) =>
        Callable(
            new CallableParts(function.AttributeLists, function.Modifiers, function.ReturnType, null, function.Identifier),
            function.TypeParameterList,
            function.ParameterList,
            function.ConstraintClauses,
            (function.Body, function.ExpressionBody, function.SemicolonToken),
            builder);

    /// <summary>Prints a constructor signature with its parameters wrapped to the width, and its body.</summary>
    /// <param name="constructor">The constructor declaration.</param>
    /// <param name="builder">Collects the constructor's own tokens.</param>
    /// <returns>The document.</returns>
    public static Doc Constructor(ConstructorDeclarationSyntax constructor, NodeBuilder builder)
    {
        Doc head = Docs.Concat(
            Attributes(constructor.AttributeLists, builder),
            builder.Tokens([.. constructor.Modifiers, constructor.Identifier]),
            ParameterListPrinter.Print(constructor.ParameterList, builder));
        Doc? initializer = constructor.Initializer is { } clause
            ? Docs.Concat(builder.Token(clause.ColonToken), Docs.Text(" "), builder.Token(clause.ThisOrBaseKeyword), ArgumentListPrinter.Print(clause.ArgumentList, builder))
            : null;
        (MemberBodyKind kind, Doc body) = DeclarationLayout.Body(constructor.Body, constructor.ExpressionBody, constructor.SemicolonToken, builder);
        return DeclarationLayout.Compose(head, [], initializer, kind, body);
    }

    private static Doc Callable(
        CallableParts parts,
        TypeParameterListSyntax? typeParameters,
        ParameterListSyntax parameters,
        SyntaxList<TypeParameterConstraintClauseSyntax> constraints,
        (BlockSyntax? Block, ArrowExpressionClauseSyntax? Expression, SyntaxToken Semicolon) ending,
        NodeBuilder builder)
    {
        var name = new List<SyntaxToken>(parts.Modifiers);
        name.AddRange(parts.ReturnType.DescendantTokens());
        if (parts.ExplicitInterface is not null)
        {
            name.AddRange(parts.ExplicitInterface.DescendantTokens());
        }

        name.Add(parts.Identifier);
        if (typeParameters is not null)
        {
            name.AddRange(typeParameters.DescendantTokens());
        }

        Doc head = Docs.Concat(builder.Tokens(name), ParameterListPrinter.Print(parameters, builder));
        var wrapped = new List<Doc>();
        foreach (TypeParameterConstraintClauseSyntax clause in constraints)
        {
            wrapped.Add(Docs.Concat(Docs.Line, builder.Tokens(clause)));
        }

        (MemberBodyKind kind, Doc body) = DeclarationLayout.Body(ending.Block, ending.Expression, ending.Semicolon, builder);
        return Docs.Concat(Attributes(parts.Attributes, builder), DeclarationLayout.Compose(head, wrapped, initializer: null, kind, body));
    }

    private static Doc ArrowBody(ArrowExpressionClauseSyntax body, NodeBuilder builder) =>
        AssignmentLayout.ArrowValue(Docs.Concat(Docs.Text(" "), builder.Token(body.ArrowToken)), body.Expression, builder);

    private static bool IsInlineAccessorList(AccessorListSyntax list) =>
        list.Accessors.All(a => a.Body is null && a.ExpressionBody is null && a.AttributeLists.Count == 0);

    private static Doc InlineAccessors(AccessorListSyntax list, NodeBuilder builder) =>
        Docs.Concat(
            Docs.Text(" "),
            builder.Token(list.OpenBraceToken),
            Docs.Text(" "),
            Docs.Join(Docs.Text(" "), list.Accessors.Select(a => builder.Tokens(a))),
            Docs.Text(" "),
            builder.Token(list.CloseBraceToken));

    private static Doc? ExpandedAccessors(AccessorListSyntax list, NodeBuilder builder)
    {
        LeadingTrivia? closing = TriviaLines.ParseLeading(list.CloseBraceToken.LeadingTrivia, atEndOfFile: false);
        OpenBrace? brace = BraceLayout.Open(list.OpenBraceToken, builder);
        if (closing is null || brace is not { } open)
        {
            builder.Reject();
            return null;
        }

        Doc close = builder.Token(list.CloseBraceToken, leadingHandled: true);
        return Docs.Concat(
            Docs.HardLine,
            open.Doc,
            Docs.Indent(Docs.Concat(Docs.HardLine, MemberList.Print([.. list.Accessors], closing))),
            Docs.HardLine,
            close);
    }

    private static Doc LinesAbove(SyntaxToken token, NodeBuilder builder)
    {
        LeadingTrivia? above = TriviaLines.ParseLeading(token.LeadingTrivia, atEndOfFile: false);
        if (above is null)
        {
            builder.Reject();
            return Docs.Empty;
        }

        builder.HandleLeading(token);
        return TriviaPrinter.Leading(above);
    }

    private sealed record CallableParts(
        SyntaxList<AttributeListSyntax> Attributes,
        SyntaxTokenList Modifiers,
        TypeSyntax ReturnType,
        ExplicitInterfaceSpecifierSyntax? ExplicitInterface,
        SyntaxToken Identifier);
}
