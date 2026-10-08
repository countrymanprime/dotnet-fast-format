using DotnetFastFormat.Core.Printers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetFastFormat.Core;

/// <summary>Decides which files are kept as written (ADR 0008, ADR 0010, ADR 0011).</summary>
internal static class VerbatimPolicy
{
    /// <summary>
    /// Returns whether the whole file is kept as written: it holds nothing but whitespace, comments and directives,
    /// its end of file carries a shape the printers do not place, or it remaps line numbers with <c>#line</c>.
    /// </summary>
    /// <param name="root">The parsed file.</param>
    /// <returns><see langword="true"/> when the file must be printed verbatim.</returns>
    public static bool KeepsWholeFile(CompilationUnitSyntax root) =>
        (!root.Externs.Any() && !root.Usings.Any() && !root.AttributeLists.Any() && !root.Members.Any())
        || TriviaLines.ParseLeading(root.EndOfFileToken.LeadingTrivia, atEndOfFile: true) is null
        || RemapsLines(root);

    /// <summary>
    /// <c>#line 100</c> and <c>#line (1,1)-(2,2)</c> change the line numbers the compiler reports for everything
    /// below them, relative to where the directive is, so reflowing the lines below would change them. <c>#line default</c>
    /// and <c>#line hidden</c> do not.
    /// </summary>
    private static bool RemapsLines(CompilationUnitSyntax root)
    {
        if (!root.ContainsDirectives)
        {
            return false;
        }

        for (DirectiveTriviaSyntax? directive = root.GetFirstDirective(); directive is not null; directive = directive.GetNextDirective())
        {
            if (directive is LineSpanDirectiveTriviaSyntax
                || (directive is LineDirectiveTriviaSyntax line && !line.Line.IsKind(SyntaxKind.DefaultKeyword) && !line.Line.IsKind(SyntaxKind.HiddenKeyword)))
            {
                return true;
            }
        }

        return false;
    }
}
