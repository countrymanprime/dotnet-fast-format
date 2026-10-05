namespace DotnetFastFormat.Core.Config;

/// <summary>One piece of a glob.</summary>
/// <param name="Kind">What the piece matches.</param>
/// <param name="Char">The character of a <see cref="GlobTokenKind.Literal"/>.</param>
/// <param name="Set">For a <see cref="GlobTokenKind.Class"/>, the inclusive character ranges as pairs: low, high, low, high.</param>
/// <param name="Negated">Whether a <see cref="GlobTokenKind.Class"/> is <c>[!seq]</c>.</param>
/// <param name="Low">The lower bound of a <see cref="GlobTokenKind.NumberRange"/>.</param>
/// <param name="High">The upper bound of a <see cref="GlobTokenKind.NumberRange"/>.</param>
internal readonly record struct GlobToken(GlobTokenKind Kind, char Char = '\0', string Set = "", bool Negated = false, long Low = 0, long High = 0)
{
    /// <summary>Gets a value indicating whether the piece can match a run of different lengths, so matching it may backtrack.</summary>
    public bool IsWildcard => Kind is GlobTokenKind.Star or GlobTokenKind.StarStar or GlobTokenKind.NumberRange;
}
