using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

/// <summary>
/// The single assertion every test layer calls, so no test can format without also checking the
/// invariants (see the <c>formatter-verification</c> skill).
/// </summary>
public static class Invariants
{
    /// <summary>Formats <paramref name="source"/>, checks every invariant, and returns the output.</summary>
    /// <exception cref="InvariantViolationException">An invariant does not hold.</exception>
    public static string FormatAndCheck(IFormatter formatter, string source)
    {
        string output = formatter.Format(source);
        VerificationResult verdict = OutputVerifier.Verify(source, output);
        if (verdict.Violation is { } violation)
        {
            throw new InvariantViolationException(Map(violation), verdict.Message);
        }

        string second = formatter.Format(output);
        if (!string.Equals(second, output, StringComparison.Ordinal))
        {
            throw new InvariantViolationException(Invariant.Idempotent, "Formatting the output again changed it.");
        }

        return output;
    }

    private static Invariant Map(OutputInvariant violation) => violation switch
    {
        OutputInvariant.ValidOutput => Invariant.ValidOutput,
        OutputInvariant.TreePreserving => Invariant.TreePreserving,
        OutputInvariant.NoLoss => Invariant.NoLoss,
        _ => throw new ArgumentOutOfRangeException(nameof(violation)),
    };
}
