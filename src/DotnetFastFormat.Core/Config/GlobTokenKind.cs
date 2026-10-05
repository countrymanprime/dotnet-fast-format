namespace DotnetFastFormat.Core.Config;

/// <summary>The pieces a glob is made of after alternatives are expanded.</summary>
internal enum GlobTokenKind
{
    /// <summary>One specific character.</summary>
    Literal,

    /// <summary><c>?</c>: any one character except <c>/</c>.</summary>
    AnyChar,

    /// <summary><c>*</c>: any run of characters except <c>/</c>.</summary>
    Star,

    /// <summary><c>**</c>: any run of characters.</summary>
    StarStar,

    /// <summary><c>[seq]</c> or <c>[!seq]</c>: one character of a set, or one that is not (and is not <c>/</c>).</summary>
    Class,

    /// <summary><c>{n..m}</c>: an integer between two bounds, written without leading zeros.</summary>
    NumberRange,
}
