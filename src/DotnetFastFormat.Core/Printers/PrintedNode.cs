using DotnetFastFormat.Core.Layout;

namespace DotnetFastFormat.Core.Printers;

/// <summary>A printed list item: its document, and the trivia around it that the list prints (ADR 0010).</summary>
/// <param name="Doc">The item itself. It already contains any trivia that is not listed in the other members.</param>
/// <param name="Leading">The own-line comments and directives to print above the item, or <see langword="null"/> when they are inside <paramref name="Doc"/>.</param>
/// <param name="TrailingComment">The comment to print after the item on its last line, or <see langword="null"/> when there is none or it is inside <paramref name="Doc"/>.</param>
internal sealed record PrintedNode(Doc Doc, LeadingTrivia? Leading, string? TrailingComment);
