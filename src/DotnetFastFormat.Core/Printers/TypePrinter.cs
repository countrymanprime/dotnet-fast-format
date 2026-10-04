using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints class, struct, interface and record declarations: header, base list, constraints and braces.</summary>
internal static class TypePrinter
{
    /// <summary>Prints <paramref name="type"/> and its members.</summary>
    /// <param name="type">The declaration.</param>
    /// <param name="builder">Collects the declaration's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the declaration has syntax this printer leaves verbatim.</returns>
    public static Doc? Print(TypeDeclarationSyntax type, NodeBuilder builder)
    {
        if (type.AttributeLists.Count > 0
            || type.BaseList?.Types.Any(t => t is PrimaryConstructorBaseTypeSyntax) == true
            || type.TypeParameterList?.DescendantNodes().OfType<AttributeListSyntax>().Any() == true)
        {
            builder.Reject();
            return null;
        }

        return Finish(type, Header(type, builder), builder);
    }

    private static Doc Header(TypeDeclarationSyntax type, NodeBuilder builder)
    {
        var name = new List<SyntaxToken>();
        name.AddRange(type.Modifiers);
        name.Add(type.Keyword);
        if (type is RecordDeclarationSyntax { ClassOrStructKeyword.RawKind: not 0 } record)
        {
            name.Add(record.ClassOrStructKeyword);
        }

        name.Add(type.Identifier);
        var head = new List<Doc> { builder.Tokens(name) };
        if (type.TypeParameterList is not null)
        {
            head.Add(builder.Tokens(type.TypeParameterList));
        }

        if (type.ParameterList is not null)
        {
            head.Add(ParameterListPrinter.Print(type.ParameterList, builder));
        }

        var wrapped = new List<Doc>();
        if (type.BaseList is not null)
        {
            wrapped.Add(BaseListDoc(type.BaseList, builder));
        }

        foreach (TypeParameterConstraintClauseSyntax clause in type.ConstraintClauses)
        {
            wrapped.Add(Docs.Concat(Docs.Line, builder.Tokens(clause)));
        }

        Doc declaration = Docs.Concat(Docs.Concat(head), Docs.Indent(Docs.Concat(wrapped)));
        return declaration;
    }

    private static Doc Finish(TypeDeclarationSyntax type, Doc declaration, NodeBuilder builder)
    {
        if (type.OpenBraceToken.IsKind(SyntaxKind.None))
        {
            return Docs.Concat(Docs.Group(declaration), builder.Token(type.SemicolonToken));
        }

        Doc open = builder.Token(type.OpenBraceToken);
        Doc close = builder.Token(type.CloseBraceToken);
        Doc semicolon = type.SemicolonToken.IsKind(SyntaxKind.None) ? Docs.Empty : builder.Token(type.SemicolonToken);
        if (type.Members.Count == 0)
        {
            // An empty body stays on the header's last line, or goes on its own line when the header wrapped.
            Doc empty = Docs.Concat(open, Docs.Text(" "), close, semicolon);
            return Docs.Group(Docs.Concat(
                declaration,
                Docs.IfBreak(Docs.Concat(Docs.HardLine, empty), Docs.Concat(Docs.Text(" "), empty))));
        }

        return Docs.Concat(
            Docs.Group(declaration),
            Docs.HardLine,
            open,
            Docs.Indent(Docs.Concat(Docs.HardLine, MemberList.Print([.. type.Members]))),
            Docs.HardLine,
            close,
            semicolon);
    }

    private static Doc BaseListDoc(BaseListSyntax list, NodeBuilder builder)
    {
        var types = new List<Doc>();
        for (int i = 0; i < list.Types.Count; i++)
        {
            Doc item = builder.Tokens(list.Types[i]);
            types.Add(i < list.Types.Count - 1
                ? Docs.Concat(item, builder.Token(list.Types.GetSeparator(i)), Docs.Line)
                : item);
        }

        return Docs.Concat(Docs.Text(" "), builder.Token(list.ColonToken), Docs.Line, Docs.Concat(types));
    }
}
