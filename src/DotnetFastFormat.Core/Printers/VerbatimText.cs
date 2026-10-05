using DotnetFastFormat.Core.Layout;
using Microsoft.CodeAnalysis;

namespace DotnetFastFormat.Core.Printers;

/// <summary>Copies a node's source text unchanged (ADR 0008).</summary>
internal static class VerbatimText
{
    /// <summary>Returns the full text of <paramref name="node"/>, comments included, without surrounding whitespace.</summary>
    /// <param name="node">The node to copy.</param>
    /// <returns>A verbatim document.</returns>
    public static Doc Of(SyntaxNode node) => Docs.Verbatim(node.ToFullString().Trim());
}
