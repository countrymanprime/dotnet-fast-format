namespace DotnetFastFormat.Core.Printers;

/// <summary>The kinds of link in a chain of member accesses and calls.</summary>
internal enum ChainLinkKind
{
    /// <summary>A member access: <c>.Name</c>, <c>?.Name</c> or <c>-&gt;Name</c>.</summary>
    Member,

    /// <summary>An invocation: <c>(arguments)</c>.</summary>
    Call,

    /// <summary>An element access: <c>[arguments]</c> or <c>?[arguments]</c>.</summary>
    Index,

    /// <summary>The null-forgiving operator <c>!</c>.</summary>
    Bang,
}
