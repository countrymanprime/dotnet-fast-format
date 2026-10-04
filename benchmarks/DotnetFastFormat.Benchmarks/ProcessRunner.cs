using System.Collections.Concurrent;
using System.Diagnostics;

namespace DotnetFastFormat.Benchmarks;

/// <summary>Runs a command and measures it.</summary>
internal static class ProcessRunner
{
    private const int TailLines = 5;

    /// <summary>Runs <paramref name="spec"/> and waits for it, killing it after <paramref name="timeout"/>.</summary>
    /// <exception cref="TimeoutException">The process did not finish in time.</exception>
    public static ProcessOutcome Run(ProcessSpec spec, TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(spec.FileName)
        {
            WorkingDirectory = spec.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in spec.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        // Some corpus repositories target Windows-only frameworks; allow restoring them on Linux.
        startInfo.Environment["EnableWindowsTargeting"] = "true";

        var lines = new ConcurrentQueue<string>();
        void Remember(object? sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                lines.Enqueue(e.Data);
                while (lines.Count > TailLines)
                {
                    lines.TryDequeue(out _);
                }
            }
        }

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += Remember;
        process.ErrorDataReceived += Remember;

        var clock = Stopwatch.StartNew();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{spec.FileName} did not finish within {timeout.TotalMinutes:0} minutes.");
        }

        process.WaitForExit();
        clock.Stop();
        return new ProcessOutcome(process.ExitCode, clock.Elapsed.TotalSeconds, string.Join(" | ", lines));
    }

    /// <summary>Runs a command and returns its trimmed standard output, or throws if it fails.</summary>
    public static string Capture(string fileName, string workingDirectory, params string[] arguments)
    {
        ProcessOutcome outcome = Run(new ProcessSpec(fileName, arguments, workingDirectory), TimeSpan.FromMinutes(5));
        return outcome.ExitCode == 0
            ? outcome.OutputTail.Split(" | ")[^1].Trim()
            : throw new InvalidOperationException($"{fileName} {string.Join(' ', arguments)} failed: {outcome.OutputTail}");
    }
}
