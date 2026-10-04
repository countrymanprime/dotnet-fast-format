namespace DotnetFastFormat.Benchmarks;

/// <summary>One timed run of one tool on one repository.</summary>
/// <param name="Succeeded">False when the tool, or its untimed preparation, exited with an error.</param>
/// <param name="Seconds">Wall-clock time of the tool run.</param>
/// <param name="ChangedFiles">Files the tool modified, so a tool that fails fast cannot look fast.</param>
/// <param name="Error">What went wrong, when it did not succeed.</param>
internal sealed record Measurement(bool Succeeded, double Seconds, int ChangedFiles, string? Error);
