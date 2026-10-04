namespace DotnetFastFormat.Benchmarks;

/// <summary>Everything one benchmark run measured.</summary>
/// <param name="Environment">Where it was measured.</param>
/// <param name="Runs">Runs per tool and repository.</param>
/// <param name="Repositories">The repositories that were formatted.</param>
/// <param name="Results">One entry per tool and repository.</param>
internal sealed record BenchmarkReport(
    EnvironmentInfo Environment,
    int Runs,
    IReadOnlyList<RepositoryInfo> Repositories,
    IReadOnlyList<ToolResult> Results);
