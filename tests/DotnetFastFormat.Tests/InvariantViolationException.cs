namespace DotnetFastFormat.Tests;

/// <summary>Thrown when a formatter breaks an invariant.</summary>
public sealed class InvariantViolationException(Invariant invariant, string message) : Exception(message)
{
    /// <summary>The invariant that was broken.</summary>
    public Invariant Invariant { get; } = invariant;
}
