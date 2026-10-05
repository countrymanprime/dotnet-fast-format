namespace DotnetFastFormat.Core.Printers;

/// <summary>One own-line piece of trivia: a comment, a directive or a block of disabled text.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Text">Its source lines, without line terminators. The first is re-indented by the printer; the rest are copied.</param>
/// <param name="BlankLineBefore">Whether the author left a blank line above it.</param>
internal sealed record TriviaLine(TriviaLineKind Kind, IReadOnlyList<string> Text, bool BlankLineBefore);
