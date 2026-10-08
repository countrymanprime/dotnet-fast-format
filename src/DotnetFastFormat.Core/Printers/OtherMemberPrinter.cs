using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints the less common members: enums, delegates, events, indexers, operators and destructors.</summary>
internal static class OtherMemberPrinter
{
    /// <summary>Prints <paramref name="node"/> when it is one of these members.</summary>
    /// <param name="node">The node.</param>
    /// <param name="builder">Collects the node's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the node is not one of these members or is kept as written.</returns>
    public static Doc? Print(SyntaxNode node, NodeBuilder builder) => node switch
    {
        EnumDeclarationSyntax @enum => Enum(@enum, builder),
        DelegateDeclarationSyntax @delegate => Delegate(@delegate, builder),
        EventFieldDeclarationSyntax field => Docs.Concat(
            MemberPrinter.Attributes(field.AttributeLists, builder),
            DeclaratorPrinter.Declaration(field.Declaration, [.. field.Modifiers, field.EventKeyword], builder),
            builder.Token(field.SemicolonToken)),
        EventDeclarationSyntax @event => Event(@event, builder),
        IndexerDeclarationSyntax indexer => Indexer(indexer, builder),
        OperatorDeclarationSyntax @operator => Operator(@operator, builder),
        ConversionOperatorDeclarationSyntax conversion => Conversion(conversion, builder),
        DestructorDeclarationSyntax destructor => Destructor(destructor, builder),
        _ => null,
    };

    private static Doc? Enum(EnumDeclarationSyntax declaration, NodeBuilder builder)
    {
        var name = new List<SyntaxToken>(declaration.Modifiers) { declaration.EnumKeyword, declaration.Identifier };
        Doc head = Docs.Concat(MemberPrinter.Attributes(declaration.AttributeLists, builder), builder.Tokens(name));
        if (declaration.BaseList is { } baseList)
        {
            head = Docs.Concat(head, Docs.Text(" "), builder.Token(baseList.ColonToken), Docs.Text(" "), builder.Tokens(baseList.Types[0]));
        }

        LeadingTrivia? closing = TriviaLines.ParseLeading(declaration.CloseBraceToken.LeadingTrivia, atEndOfFile: false);
        OpenBrace? brace = BraceLayout.Open(declaration.OpenBraceToken, builder);
        if (closing is null || brace is not { } open)
        {
            builder.Reject();
            return null;
        }

        Doc close = builder.Token(declaration.CloseBraceToken, leadingHandled: true);
        Doc semicolon = declaration.SemicolonToken.IsKind(SyntaxKind.None) ? Docs.Empty : builder.Token(declaration.SemicolonToken);
        if (declaration.Members.Count == 0 && closing.Lines.Count == 0)
        {
            return Docs.Concat(head, open.IsPlain ? Docs.Text(" ") : Docs.HardLine, open.Doc, open.HasComment ? Docs.HardLine : Docs.Text(" "), close, semicolon);
        }

        Doc? members = EnumMembers(declaration.Members, closing, builder);
        return members is null
            ? null
            : Docs.Concat(head, Docs.HardLine, open.Doc, Docs.Indent(Docs.Concat(Docs.HardLine, members)), Docs.HardLine, close, semicolon);
    }

    /// <summary>
    /// Prints enum members one per line. A member's comma, and the comment after it, belong to the member, so the members
    /// are not a plain list of siblings.
    /// </summary>
    private static Doc? EnumMembers(SeparatedSyntaxList<EnumMemberDeclarationSyntax> members, LeadingTrivia closing, NodeBuilder builder)
    {
        var parts = new List<Doc>();
        for (int i = 0; i < members.Count; i++)
        {
            EnumMemberDeclarationSyntax member = members[i];
            LeadingTrivia? above = TriviaLines.ParseLeading(member.GetFirstToken().LeadingTrivia, atEndOfFile: false);
            bool hasComma = i < members.SeparatorCount;
            SyntaxToken tail = hasComma ? members.GetSeparator(i) : member.GetLastToken();
            (bool clean, string? comment) = TriviaLines.ParseTrailing(tail.TrailingTrivia);
            if (above is null || !clean)
            {
                builder.Reject();
                return null;
            }

            builder.HandleLeading(member.GetFirstToken());
            if (i > 0)
            {
                parts.Add(Docs.HardLine);
                if (TriviaRules.BlankLinesBefore(member) > 0)
                {
                    parts.Add(Docs.HardLine);
                }
            }

            parts.Add(TriviaPrinter.Leading(above));
            builder.HandleTrailing(tail);
            parts.Add(EnumMember(member, builder));
            if (hasComma)
            {
                parts.Add(builder.Token(tail));
            }

            parts.Add(comment is null ? Docs.Empty : Docs.LineSuffix(" " + comment));
        }

        parts.Add(TriviaPrinter.Closing(closing, hasItemsAbove: members.Count > 0));
        return Docs.Concat(parts);
    }

    private static Doc EnumMember(EnumMemberDeclarationSyntax member, NodeBuilder builder)
    {
        Doc attributes = MemberPrinter.Attributes(member.AttributeLists, builder);
        Doc name = builder.Token(member.Identifier);
        return member.EqualsValue is not { } value
            ? Docs.Concat(attributes, name)
            : Docs.Concat(attributes, AssignmentLayout.Print(name, value.EqualsToken, value.Value, builder));
    }

    private static Doc Delegate(DelegateDeclarationSyntax declaration, NodeBuilder builder)
    {
        var name = new List<SyntaxToken>(declaration.Modifiers) { declaration.DelegateKeyword };
        name.AddRange(declaration.ReturnType.DescendantTokens());
        name.Add(declaration.Identifier);
        if (declaration.TypeParameterList is not null)
        {
            name.AddRange(declaration.TypeParameterList.DescendantTokens());
        }

        var wrapped = new List<Doc>();
        foreach (TypeParameterConstraintClauseSyntax clause in declaration.ConstraintClauses)
        {
            wrapped.Add(Docs.Concat(Docs.Line, builder.Tokens(clause)));
        }

        Doc signature = Docs.Concat(builder.Tokens(name), ParameterListPrinter.Print(declaration.ParameterList, builder), Docs.Indent(Docs.Concat(wrapped)));
        return Docs.Concat(MemberPrinter.Attributes(declaration.AttributeLists, builder), Docs.Group(signature), builder.Token(declaration.SemicolonToken));
    }

    private static Doc? Event(EventDeclarationSyntax declaration, NodeBuilder builder)
    {
        var tokens = new List<SyntaxToken>(declaration.Modifiers) { declaration.EventKeyword };
        tokens.AddRange(declaration.Type.DescendantTokens());
        if (declaration.ExplicitInterfaceSpecifier is not null)
        {
            tokens.AddRange(declaration.ExplicitInterfaceSpecifier.DescendantTokens());
        }

        tokens.Add(declaration.Identifier);
        Doc head = Docs.Concat(MemberPrinter.Attributes(declaration.AttributeLists, builder), builder.Tokens(tokens));
        return MemberPrinter.Accessors(head, declaration.AccessorList, expressionBody: null, default, builder);
    }

    private static Doc? Indexer(IndexerDeclarationSyntax indexer, NodeBuilder builder)
    {
        var tokens = new List<SyntaxToken>(indexer.Modifiers);
        tokens.AddRange(indexer.Type.DescendantTokens());
        if (indexer.ExplicitInterfaceSpecifier is not null)
        {
            tokens.AddRange(indexer.ExplicitInterfaceSpecifier.DescendantTokens());
        }

        tokens.Add(indexer.ThisKeyword);
        Doc head = Docs.Concat(
            MemberPrinter.Attributes(indexer.AttributeLists, builder),
            builder.Tokens(tokens),
            ParameterListPrinter.Bracketed(indexer.ParameterList, builder));
        return MemberPrinter.Accessors(head, indexer.AccessorList, indexer.ExpressionBody, indexer.SemicolonToken, builder);
    }

    private static Doc Operator(OperatorDeclarationSyntax declaration, NodeBuilder builder)
    {
        var name = new List<SyntaxToken>(declaration.Modifiers);
        name.AddRange(declaration.ReturnType.DescendantTokens());
        var operatorName = new List<Doc> { builder.Token(declaration.OperatorKeyword), Docs.Text(" ") };
        if (!declaration.CheckedKeyword.IsKind(SyntaxKind.None))
        {
            operatorName.Add(builder.Token(declaration.CheckedKeyword));
            operatorName.Add(Docs.Text(" "));
        }

        operatorName.Add(builder.Token(declaration.OperatorToken));
        Doc head = Docs.Concat(
            builder.Tokens(name),
            Docs.Text(" "),
            Docs.Concat(operatorName),
            ParameterListPrinter.Print(declaration.ParameterList, builder));
        (MemberBodyKind kind, Doc body) = DeclarationLayout.Body(declaration.Body, declaration.ExpressionBody, declaration.SemicolonToken, builder);
        return Docs.Concat(MemberPrinter.Attributes(declaration.AttributeLists, builder), DeclarationLayout.Compose(head, [], initializer: null, kind, body));
    }

    private static Doc Conversion(ConversionOperatorDeclarationSyntax declaration, NodeBuilder builder)
    {
        var name = new List<SyntaxToken>(declaration.Modifiers) { declaration.ImplicitOrExplicitKeyword, declaration.OperatorKeyword };
        if (!declaration.CheckedKeyword.IsKind(SyntaxKind.None))
        {
            name.Add(declaration.CheckedKeyword);
        }

        name.AddRange(declaration.Type.DescendantTokens());
        Doc head = Docs.Concat(builder.Tokens(name), ParameterListPrinter.Print(declaration.ParameterList, builder));
        (MemberBodyKind kind, Doc body) = DeclarationLayout.Body(declaration.Body, declaration.ExpressionBody, declaration.SemicolonToken, builder);
        return Docs.Concat(MemberPrinter.Attributes(declaration.AttributeLists, builder), DeclarationLayout.Compose(head, [], initializer: null, kind, body));
    }

    private static Doc Destructor(DestructorDeclarationSyntax declaration, NodeBuilder builder)
    {
        Doc modifiers = declaration.Modifiers.Count == 0 ? Docs.Empty : Docs.Concat(builder.Tokens(declaration.Modifiers), Docs.Text(" "));
        Doc head = Docs.Concat(
            modifiers,
            builder.Token(declaration.TildeToken),
            builder.Token(declaration.Identifier),
            ParameterListPrinter.Print(declaration.ParameterList, builder));
        (MemberBodyKind kind, Doc body) = DeclarationLayout.Body(declaration.Body, declaration.ExpressionBody, declaration.SemicolonToken, builder);
        return Docs.Concat(MemberPrinter.Attributes(declaration.AttributeLists, builder), DeclarationLayout.Compose(head, [], initializer: null, kind, body));
    }
}
