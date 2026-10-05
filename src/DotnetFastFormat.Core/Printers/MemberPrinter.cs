using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints fields, auto-properties, methods and constructors. Initializer values and method bodies are kept as
/// written until their printers exist.
/// </summary>
internal static class MemberPrinter
{
    /// <summary>Prints a field.</summary>
    /// <param name="field">The field declaration.</param>
    /// <param name="builder">Collects the field's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the field is kept as written.</returns>
    public static Doc? Field(FieldDeclarationSyntax field, NodeBuilder builder)
    {
        if (field.AttributeLists.Count > 0 || field.Declaration.Variables.Any(v => v.ArgumentList is not null))
        {
            builder.Reject();
            return null;
        }

        var declarators = new List<Doc>();
        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
        {
            declarators.Add(Docs.Concat(builder.Token(variable.Identifier), Initializer(variable.Initializer, builder)));
        }

        var commas = new List<SyntaxToken>(field.Declaration.Variables.GetSeparators());
        var parts = new List<Doc>();
        for (int i = 0; i < declarators.Count; i++)
        {
            parts.Add(declarators[i]);
            if (i < commas.Count)
            {
                parts.Add(builder.Token(commas[i]));
                parts.Add(Docs.Text(" "));
            }
        }

        return Docs.Concat(
            builder.Tokens([.. field.Modifiers, .. field.Declaration.Type.DescendantTokens()]),
            Docs.Text(" "),
            Docs.Concat(parts),
            builder.Token(field.SemicolonToken));
    }

    /// <summary>Prints a property with no accessor bodies, or with an expression body.</summary>
    /// <param name="property">The property declaration.</param>
    /// <param name="builder">Collects the property's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the property is kept as written.</returns>
    public static Doc? Property(PropertyDeclarationSyntax property, NodeBuilder builder)
    {
        bool auto = property.AccessorList is { } list
            && property.ExpressionBody is null
            && list.Accessors.All(a => a.Body is null && a.ExpressionBody is null && a.AttributeLists.Count == 0);
        if (property.AttributeLists.Count > 0 || !(auto || (property.ExpressionBody is not null && property.AccessorList is null)))
        {
            builder.Reject();
            return null;
        }

        var tokens = new List<SyntaxToken>(property.Modifiers);
        tokens.AddRange(property.Type.DescendantTokens());
        if (property.ExplicitInterfaceSpecifier is not null)
        {
            tokens.AddRange(property.ExplicitInterfaceSpecifier.DescendantTokens());
        }

        tokens.Add(property.Identifier);
        Doc head = builder.Tokens(tokens);
        if (!auto)
        {
            return Docs.Concat(
                head,
                Docs.Text(" "),
                builder.Token(property.ExpressionBody!.ArrowToken),
                Docs.Text(" "),
                VerbatimText.Of(property.ExpressionBody.Expression),
                builder.Token(property.SemicolonToken));
        }

        AccessorListSyntax accessors = property.AccessorList!;
        Doc list2 = Docs.Concat(
            Docs.Text(" "),
            builder.Token(accessors.OpenBraceToken),
            Docs.Text(" "),
            Docs.Join(Docs.Text(" "), accessors.Accessors.Select(a => builder.Tokens(a))),
            Docs.Text(" "),
            builder.Token(accessors.CloseBraceToken));
        Doc value = property.Initializer is null
            ? Docs.Empty
            : Docs.Concat(Docs.Text(" "), Initializer(property.Initializer, builder, leadingSpace: false), builder.Token(property.SemicolonToken));
        return Docs.Concat(head, list2, value);
    }

    /// <summary>Prints a method signature with its parameters wrapped to the width.</summary>
    /// <param name="method">The method declaration.</param>
    /// <param name="builder">Collects the method's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the method is kept as written.</returns>
    public static Doc? Method(MethodDeclarationSyntax method, NodeBuilder builder)
    {
        if (method.AttributeLists.Count > 0)
        {
            builder.Reject();
            return null;
        }

        var name = new List<SyntaxToken>(method.Modifiers);
        name.AddRange(method.ReturnType.DescendantTokens());
        if (method.ExplicitInterfaceSpecifier is not null)
        {
            name.AddRange(method.ExplicitInterfaceSpecifier.DescendantTokens());
        }

        name.Add(method.Identifier);
        if (method.TypeParameterList is not null)
        {
            name.AddRange(method.TypeParameterList.DescendantTokens());
        }

        Doc head = Docs.Concat(builder.Tokens(name), ParameterListPrinter.Print(method.ParameterList, builder));
        List<Doc> wrapped = Constraints(method.ConstraintClauses, builder);
        (MemberBodyKind kind, Doc body) = DeclarationLayout.Body(method.Body, method.ExpressionBody, method.SemicolonToken, builder);
        return DeclarationLayout.Compose(head, wrapped, initializer: null, kind, body);
    }

    /// <summary>Prints a constructor signature with its parameters wrapped to the width.</summary>
    /// <param name="constructor">The constructor declaration.</param>
    /// <param name="builder">Collects the constructor's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the constructor is kept as written.</returns>
    public static Doc? Constructor(ConstructorDeclarationSyntax constructor, NodeBuilder builder)
    {
        if (constructor.AttributeLists.Count > 0)
        {
            builder.Reject();
            return null;
        }

        Doc head = Docs.Concat(
            builder.Tokens([.. constructor.Modifiers, constructor.Identifier]),
            ParameterListPrinter.Print(constructor.ParameterList, builder));
        Doc? initializer = constructor.Initializer is null ? null : VerbatimText.Of(constructor.Initializer);
        (MemberBodyKind kind, Doc body) = DeclarationLayout.Body(constructor.Body, constructor.ExpressionBody, constructor.SemicolonToken, builder);
        return DeclarationLayout.Compose(head, [], initializer, kind, body);
    }

    private static List<Doc> Constraints(SyntaxList<TypeParameterConstraintClauseSyntax> clauses, NodeBuilder builder)
    {
        var wrapped = new List<Doc>();
        foreach (TypeParameterConstraintClauseSyntax clause in clauses)
        {
            wrapped.Add(Docs.Concat(Docs.Line, builder.Tokens(clause)));
        }

        return wrapped;
    }

    private static Doc Initializer(EqualsValueClauseSyntax? clause, NodeBuilder builder, bool leadingSpace = true) =>
        clause is null
            ? Docs.Empty
            : Docs.Concat(
                leadingSpace ? Docs.Text(" ") : Docs.Empty,
                builder.Token(clause.EqualsToken),
                Docs.Text(" "),
                VerbatimText.Of(clause.Value));
}
