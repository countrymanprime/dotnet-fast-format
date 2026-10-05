namespace DotnetFastFormat.Core.Printers;

/// <summary>
/// The kind of a sibling for blank-line purposes. Two neighbours of the same kind other than
/// <see cref="Other"/> keep the author's choice of no blank line or one; anything else gets one.
/// </summary>
internal enum MemberKind
{
    /// <summary>A member that always has a blank line around it.</summary>
    Other,

    /// <summary>A using directive or extern alias.</summary>
    Using,

    /// <summary>A field declaration.</summary>
    Field,

    /// <summary>A property whose accessors have no bodies.</summary>
    AutoProperty,

    /// <summary>A method with no body.</summary>
    Signature,

    /// <summary>An event declared without accessors.</summary>
    Event,

    /// <summary>A top-level statement.</summary>
    Statement,
}
