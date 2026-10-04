namespace DotnetFastFormat.Benchmarks;

/// <summary>Median, minimum and maximum of a set of timings, in seconds.</summary>
internal sealed record Summary(double Median, double Min, double Max);
