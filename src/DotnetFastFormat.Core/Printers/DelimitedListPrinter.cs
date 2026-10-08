using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// Prints elements between two delimiters, such as an initializer or a collection expression: on one line when they
/// fit, otherwise one element per line, indented one level, with the closing delimiter on its own line (ADR 0013).
/// </summary>
internal static class DelimitedListPrinter
{
    /// <summary>Prints the list.</summary>
    /// <param name="open">The opening delimiter.</param>
    /// <param name="close">The closing delimiter.</param>
    /// <param name="items">The element documents.</param>
    /// <param name="separators">The comma tokens between elements, including a trailing one when the source has it.</param>
    /// <param name="builder">Collects the separators' tokens.</param>
    /// <param name="padding">What goes inside the delimiters when flat: <see cref="Docs.Line"/> for a space, <see cref="Docs.SoftLine"/> for nothing.</param>
    /// <param name="before">A line that goes before the opening delimiter and breaks with the list, so the delimiter moves to its own line (Allman), or <see langword="null"/>.</param>
    /// <returns>The document. A trailing comma in the source is a token that stays, so it forces the one-per-line form.</returns>
    public static Doc Print(Doc open, Doc close, IReadOnlyList<Doc> items, IReadOnlyList<SyntaxToken> separators, NodeBuilder builder, Doc padding, Doc? before)
    {
        var body = new List<Doc>();
        for (int i = 0; i < items.Count; i++)
        {
            body.Add(items[i]);
            if (i < separators.Count)
            {
                body.Add(builder.Token(separators[i]));
                if (i < items.Count - 1)
                {
                    body.Add(Docs.Line);
                }
            }
        }

        return Docs.Group(
            Docs.Concat(before ?? Docs.Empty, open, Docs.Indent(Docs.Concat(padding, Docs.Concat(body))), padding, close),
            forceBreak: separators.Count == items.Count);
    }

    /// <summary>Returns the separators of <paramref name="list"/>, including a trailing one.</summary>
    /// <typeparam name="T">The node type of the list.</typeparam>
    /// <param name="list">The list.</param>
    /// <returns>The comma tokens.</returns>
    public static IReadOnlyList<SyntaxToken> Separators<T>(SeparatedSyntaxList<T> list)
        where T : SyntaxNode => [.. list.GetSeparators()];
}
