namespace DotnetFastFormat.Benchmarks;

/// <summary>A command to run.</summary>
/// <param name="FileName">The executable.</param>
/// <param name="Arguments">Its arguments.</param>
/// <param name="WorkingDirectory">The directory to run in.</param>
internal sealed record ProcessSpec(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory);
