using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core.Printers;

/// <summary>One link of a chain such as <c>a.B(x)[0]!</c>.</summary>
/// <param name="Kind">What the link is.</param>
/// <param name="Operator">The <c>.</c> or <c>-&gt;</c> of a member link, or the <c>!</c> of a bang link; unused otherwise.</param>
/// <param name="Name">The name of a member link.</param>
/// <param name="Arguments">The argument list of a call link or the bracketed list of an index link.</param>
/// <param name="Question">The <c>?</c> before a member or index link of a conditional access, or a token that is none.</param>
internal sealed record ChainLink(
    ChainLinkKind Kind,
    SyntaxToken Operator,
    SimpleNameSyntax? Name,
    SyntaxNode? Arguments,
    SyntaxToken Question = default);
