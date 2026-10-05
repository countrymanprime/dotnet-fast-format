using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core;

/// <summary>Decides which files are kept as written (ADR 0008).</summary>
internal static class VerbatimPolicy
{
    /// <summary>
    /// Returns whether the whole file is kept as written: it has conditional or region directives,
    /// no declarations, or a comment after its last token.
    /// </summary>
    /// <param name="root">The parsed file.</param>
    /// <returns><see langword="true"/> when the file must be printed verbatim.</returns>
    public static bool KeepsWholeFile(CompilationUnitSyntax root) =>
        HasConditionalDirective(root)
        || !root.Members.Any()
        || root.EndOfFileToken.LeadingTrivia.Any(IsComment);

    private static bool HasConditionalDirective(CompilationUnitSyntax root) =>
        root.DescendantTrivia(descendIntoTrivia: false).Any(t => t.Kind() is
            SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia
            or SyntaxKind.EndIfDirectiveTrivia or SyntaxKind.RegionDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia);

    private static bool IsComment(SyntaxTrivia trivia) =>
        trivia.Kind() is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia
            or SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia;
}
