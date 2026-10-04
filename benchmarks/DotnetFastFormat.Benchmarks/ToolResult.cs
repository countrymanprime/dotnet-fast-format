namespace DotnetFastFormat.Benchmarks;

/// <summary>All runs of one tool on one repository.</summary>
/// <param name="Repository">The corpus repository name.</param>
/// <param name="Tool">The tool name.</param>
/// <param name="Runs">One entry per run.</param>
internal sealed record ToolResult(string Repository, string Tool, IReadOnlyList<Measurement> Runs);
