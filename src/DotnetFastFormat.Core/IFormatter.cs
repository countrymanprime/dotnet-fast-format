namespace DotnetFastFormat.Core;

/// <summary>Formats C# source text.</summary>
/// <remarks>
/// Implementations must satisfy the invariants in <c>AGENTS.md</c>: idempotent, tree-preserving
/// and no loss of comments or directives. The test suite checks every implementation against them.
/// </remarks>
public interface IFormatter
{
    /// <summary>Returns <paramref name="source"/> formatted.</summary>
    /// <param name="source">C# source text.</param>
    /// <returns>The formatted text.</returns>
    string Format(string source);
}
