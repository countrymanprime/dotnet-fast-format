namespace DotnetFastFormat.Core.Layout;

/// <summary>What a <see cref="LineDoc"/> prints when its group is flat.</summary>
internal enum LineKind
{
    /// <summary>A space when flat, a line break when broken.</summary>
    Normal,

    /// <summary>Nothing when flat, a line break when broken.</summary>
    Soft,

    /// <summary>Always a line break, and every enclosing group breaks.</summary>
    Hard,
}
