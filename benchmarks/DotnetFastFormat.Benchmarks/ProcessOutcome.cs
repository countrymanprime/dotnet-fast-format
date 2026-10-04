namespace DotnetFastFormat.Benchmarks;

/// <summary>The result of running a <see cref="ProcessSpec"/>.</summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Seconds">Wall-clock time from start to exit.</param>
/// <param name="OutputTail">The last lines the process wrote, for diagnosing failures.</param>
internal sealed record ProcessOutcome(int ExitCode, double Seconds, string OutputTail);
