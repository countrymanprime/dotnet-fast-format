namespace DotnetFastFormat.Core.Printers;

/// <summary>The trivia before a token, when it is only whitespace and own-line pieces (ADR 0010).</summary>
/// <param name="Lines">The own-line pieces, in order.</param>
/// <param name="BlankLineAfter">Whether the author left a blank line between the last piece (or the previous line) and the token.</param>
internal sealed record LeadingTrivia(IReadOnlyList<TriviaLine> Lines, bool BlankLineAfter);
