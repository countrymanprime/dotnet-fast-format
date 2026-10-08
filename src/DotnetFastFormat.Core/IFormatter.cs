namespace DotnetFastFormat.Core;

/// <summary>Formats C# source text.</summary>
/// <remarks>
/// Implementations must satisfy requirements FMT-001, FMT-002, FMT-003 and FMT-005 in
/// <c>docs/requirements/core.md</c>: idempotent, tree-preserving, no loss of comments or
/// directives, and valid output. The test suite checks every implementation against them.
/// </remarks>
public interface IFormatter
{
    /// <summary>Returns <paramref name="source"/> formatted with <see cref="FormatOptions.Default"/>.</summary>
    /// <param name="source">C# source text.</param>
    /// <returns>The formatted text.</returns>
    string Format(string source) => Format(source, FormatOptions.Default);

    /// <summary>Returns <paramref name="source"/> formatted with <paramref name="options"/>.</summary>
    /// <param name="source">C# source text.</param>
    /// <param name="options">The settings, normally resolved from <c>.editorconfig</c>.</param>
    /// <returns>The formatted text.</returns>
    string Format(string source, FormatOptions options);
}
