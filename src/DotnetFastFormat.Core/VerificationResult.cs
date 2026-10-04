namespace DotnetFastFormat.Core;

/// <summary>The outcome of <see cref="OutputVerifier.Verify"/>.</summary>
/// <param name="Violation">The first invariant that failed, or <see langword="null"/> when all hold.</param>
/// <param name="Message">A description of the failure, or an empty string on success.</param>
public sealed record VerificationResult(OutputInvariant? Violation, string Message)
{
    /// <summary>Gets a value indicating whether every invariant holds.</summary>
    public bool Succeeded => Violation is null;

    /// <summary>Gets the result for output that passes every check.</summary>
    public static VerificationResult Success { get; } = new(null, string.Empty);
}
