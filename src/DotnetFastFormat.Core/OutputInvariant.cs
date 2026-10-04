namespace DotnetFastFormat.Core;

/// <summary>The invariants <see cref="OutputVerifier"/> checks, in the order they are checked.</summary>
public enum OutputInvariant
{
    /// <summary>The output parses with no error the input did not have.</summary>
    ValidOutput,

    /// <summary>The output has the same tokens as the input, ignoring trivia.</summary>
    TreePreserving,

    /// <summary>Every comment and directive survives, in place relative to the code.</summary>
    NoLoss,
}
