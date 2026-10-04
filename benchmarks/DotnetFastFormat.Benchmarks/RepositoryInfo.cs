namespace DotnetFastFormat.Benchmarks;

/// <summary>The size of one corpus repository.</summary>
/// <param name="Name">The corpus repository name.</param>
/// <param name="CsFiles">Number of <c>.cs</c> files.</param>
/// <param name="Megabytes">Total size of the <c>.cs</c> files in megabytes.</param>
internal sealed record RepositoryInfo(string Name, int CsFiles, double Megabytes);
