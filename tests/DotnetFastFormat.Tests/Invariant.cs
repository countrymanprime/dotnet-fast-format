namespace DotnetFastFormat.Tests;

/// <summary>The formatter invariants from <c>AGENTS.md</c>, in the order they are checked.</summary>
public enum Invariant
{
    /// <summary>The output parses with no diagnostics the input did not have.</summary>
    ValidOutput,

    /// <summary>The output has the same tokens as the input, ignoring trivia.</summary>
    TreePreserving,

    /// <summary>Every comment and directive survives, in place relative to the code.</summary>
    NoLoss,

    /// <summary>Formatting the output again gives the same text.</summary>
    Idempotent,
}
