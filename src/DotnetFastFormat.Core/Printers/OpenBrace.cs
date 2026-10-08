using DotnetFastFormat.Core.Layout;

namespace DotnetFastFormat.Core.Printers;

/// <summary>An opening brace that starts its line, with the trivia around it that is printed with it.</summary>
/// <param name="Doc">The own-line comments and directives above the brace, the brace, and a comment after it on its line.</param>
/// <param name="HasComment">Whether a comment follows the brace on its line.</param>
/// <param name="HasLinesAbove">Whether comments or directives sit above the brace, on lines of their own.</param>
internal readonly record struct OpenBrace(Doc Doc, bool HasComment, bool HasLinesAbove)
{
    /// <summary>Gets a value indicating whether nothing but the brace is on its line and above it, so an empty body may print <c>{ }</c> on the line of its header.</summary>
    public bool IsPlain => !HasComment && !HasLinesAbove;
}
