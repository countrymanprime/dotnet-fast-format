using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints chains of member accesses, calls and element accesses (ADR 0013). A chain is a head (the root and what is glued
/// to it) followed by groups, each a member access and the calls after it. Few groups stay on one line with line
/// breaks only inside argument lists; more groups go on one line when they fit and otherwise one group per line,
/// indented one level, starting with the member access.
/// </summary>
internal static class ChainPrinter
{
    private const int ShortNameLength = 4;

    /// <summary>Prints a chain.</summary>
    /// <param name="expression">An invocation, member access, element access, conditional access or null-forgiving expression.</param>
    /// <param name="builder">Collects the chain's own tokens.</param>
    /// <param name="context">Where the chain sits.</param>
    /// <returns>The document.</returns>
    public static Doc Print(ExpressionSyntax expression, NodeBuilder builder, ExprContext context)
    {
        var links = new List<ChainLink>();
        ExpressionSyntax root = ChainFlattener.Flatten(expression, links)!;
        Doc rootDoc = ExpressionPrinter.Print(root, builder);
        int lastCall = links.FindLastIndex(link => link.Kind == ChainLinkKind.Call);
        if (lastCall < 0)
        {
            return Docs.Concat(rootDoc, Docs.Concat(links.Select(link => LinkDoc(link, builder))));
        }

        Doc chain = Chain(root, rootDoc, links.GetRange(0, lastCall + 1), builder, context);
        return Docs.Concat(chain, Docs.Concat(links.Skip(lastCall + 1).Select(link => LinkDoc(link, builder))));
    }

    private static Doc Chain(ExpressionSyntax root, Doc rootDoc, List<ChainLink> chain, NodeBuilder builder, ExprContext context)
    {
        List<Doc> docs = [.. chain.Select(link => LinkDoc(link, builder))];
        var head = new List<Doc> { rootDoc };
        int i = 0;
        while (i < chain.Count && chain[i].Kind != ChainLinkKind.Member)
        {
            head.Add(docs[i++]);
        }

        if (chain[0].Kind != ChainLinkKind.Call)
        {
            while (i + 1 < chain.Count && chain[i].Kind == ChainLinkKind.Member && chain[i + 1].Kind == ChainLinkKind.Member)
            {
                head.Add(docs[i++]);
            }
        }

        int headLinks = i;
        List<Doc> groups = Groups(chain, docs, i);
        bool merge = groups.Count > 0 && ShouldMerge(root, headLinks, context);
        Doc headDoc = Docs.Concat(head);
        Doc oneLine = Docs.Concat(headDoc, Docs.Concat(groups));
        if (1 + groups.Count <= (merge ? 3 : 2))
        {
            return oneLine;
        }

        int merged = merge ? 1 : 0;
        Doc expanded = Docs.Concat(
            headDoc,
            merge ? groups[0] : Docs.Empty,
            Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Join(Docs.HardLine, groups.Skip(merged)))));
        if (groups.Take(groups.Count - 1).Any(group => group.WillBreak))
        {
            return expanded;
        }

        Doc choice = Docs.Conditional(oneLine, expanded);
        return oneLine.WillBreak ? Docs.Concat(Docs.Group(Docs.Empty, forceBreak: true), choice) : choice;
    }

    /// <summary>Splits what follows the head into groups: a group ends when a member access follows a call.</summary>
    private static List<Doc> Groups(List<ChainLink> chain, List<Doc> docs, int start)
    {
        var groups = new List<Doc>();
        var current = new List<Doc>();
        bool seenCall = false;
        for (int i = start; i < chain.Count; i++)
        {
            if (seenCall && chain[i].Kind == ChainLinkKind.Member)
            {
                groups.Add(Docs.Concat(current));
                current = [];
                seenCall = false;
            }

            seenCall |= chain[i].Kind == ChainLinkKind.Call;
            current.Add(docs[i]);
        }

        if (current.Count > 0)
        {
            groups.Add(Docs.Concat(current));
        }

        return groups;
    }

    /// <summary>
    /// Returns whether the first group stays on the head's line: <c>this.A()</c>, <c>Type.Method()</c>,
    /// <c>string.Join()</c> and, as a statement, <c>sb.Append()</c> read better that way. Only a head that is
    /// the root alone qualifies: in C# every member name is capitalized, so a member name says nothing.
    /// </summary>
    private static bool ShouldMerge(ExpressionSyntax root, int headLinks, ExprContext context)
    {
        if (headLinks > 0)
        {
            return false;
        }

        return root switch
        {
            ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax => true,
            IdentifierNameSyntax name => IsFactory(name.Identifier.Text)
                || (context == ExprContext.Statement && name.Identifier.Text.Length <= ShortNameLength),
            GenericNameSyntax name => IsFactory(name.Identifier.Text),
            _ => false,
        };
    }

    private static bool IsFactory(string name) => name.Length > 0 && (char.IsUpper(name[0]) || name[0] == '_');

    private static Doc LinkDoc(ChainLink link, NodeBuilder builder)
    {
        Doc question = link.Question.IsKind(SyntaxKind.None) ? Docs.Empty : builder.Token(link.Question);
        return link.Kind switch
        {
            ChainLinkKind.Member => Docs.Concat(question, builder.Token(link.Operator), builder.Tokens(link.Name)),
            ChainLinkKind.Call => ArgumentListPrinter.Print((ArgumentListSyntax)link.Arguments!, builder),
            ChainLinkKind.Index => Docs.Concat(question, ArgumentListPrinter.Bracketed((BracketedArgumentListSyntax)link.Arguments!, builder)),
            _ => builder.Token(link.Operator),
        };
    }
}
