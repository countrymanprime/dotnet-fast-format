using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Prints block and file-scoped namespaces.</summary>
internal static class NamespacePrinter
{
    /// <summary>Prints <paramref name="ns"/> and its members.</summary>
    /// <param name="ns">The namespace.</param>
    /// <param name="builder">Collects the namespace's own tokens.</param>
    /// <returns>The document, or <see langword="null"/> when the namespace has syntax this printer leaves verbatim.</returns>
    public static Doc? Print(BaseNamespaceDeclarationSyntax ns, NodeBuilder builder)
    {
        if (ns.AttributeLists.Count > 0)
        {
            builder.Reject();
            return null;
        }

        Doc header = Docs.Concat(builder.Token(ns.NamespaceKeyword), Docs.Text(" "), builder.Tokens(ns.Name));
        var children = new List<SyntaxNode>();
        children.AddRange(ns.Externs);
        children.AddRange(ns.Usings);
        children.AddRange(ns.Members);
        children.Sort((a, b) => a.SpanStart.CompareTo(b.SpanStart));

        if (ns is FileScopedNamespaceDeclarationSyntax fileScoped)
        {
            Doc semicolon = builder.Token(fileScoped.SemicolonToken);
            return children.Count == 0
                ? Docs.Concat(header, semicolon)
                : Docs.Concat(header, semicolon, Docs.HardLine, Docs.HardLine, MemberList.Print(children));
        }

        var block = (NamespaceDeclarationSyntax)ns;
        Doc open = builder.Token(block.OpenBraceToken);
        Doc close = builder.Token(block.CloseBraceToken);
        Doc trailing = block.SemicolonToken.IsKind(SyntaxKind.None)
            ? Docs.Empty
            : builder.Token(block.SemicolonToken);

        return children.Count == 0
            ? Docs.Concat(header, Docs.Text(" "), open, Docs.Text(" "), close, trailing)
            : Docs.Concat(
                header,
                Docs.HardLine,
                open,
                Docs.Indent(Docs.Concat(Docs.HardLine, MemberList.Print(children))),
                Docs.HardLine,
                close,
                trailing);
    }
}
