namespace DotnetFastFormat.Core.Printers;

/// <summary>How a method or constructor ends.</summary>
internal enum MemberBodyKind
{
    /// <summary>A bare <c>;</c>: no body.</summary>
    Semicolon,

    /// <summary>An expression body, <c>=&gt; value;</c>, with the value kept as written.</summary>
    Expression,

    /// <summary>A block whose statements are kept as written.</summary>
    Block,

    /// <summary>A block with nothing in it, printed <c>{ }</c>.</summary>
    EmptyBlock,
}
