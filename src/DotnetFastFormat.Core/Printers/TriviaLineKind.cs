namespace DotnetFastFormat.Core.Printers;

/// <summary>What an own-line piece of trivia is, because each kind is indented differently.</summary>
internal enum TriviaLineKind
{
    /// <summary>A comment or documentation comment: re-indented with the code.</summary>
    Comment,

    /// <summary>A preprocessor directive other than a region: written at column 0.</summary>
    Directive,

    /// <summary>A <c>#region</c> or <c>#endregion</c> directive: indented with the code.</summary>
    Region,

    /// <summary>Text of an inactive preprocessor branch: copied from column 0.</summary>
    DisabledText,
}
