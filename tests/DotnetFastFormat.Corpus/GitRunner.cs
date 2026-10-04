using System.Diagnostics;
using System.Text;

namespace DotnetFastFormat.Corpus;

/// <summary>Runs the <c>git</c> command line without ever prompting or pulling LFS objects.</summary>
public static class GitRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

    /// <summary>Runs git and returns its output, or throws if it fails.</summary>
    public static string RunChecked(string workingDirectory, params string[] arguments)
    {
        (int exitCode, string output, string error) = Run(workingDirectory, arguments);
        return exitCode == 0
            ? output
            : throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed with exit code {exitCode}: {error.Trim()}");
    }

    /// <summary>Runs git and returns its exit code and output.</summary>
    public static (int ExitCode, string Output, string Error) Run(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_LFS_SKIP_SMUDGE"] = "1";

        var output = new StringBuilder();
        var error = new StringBuilder();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => output.AppendLine(e.Data);
        process.ErrorDataReceived += (_, e) => error.AppendLine(e.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"git {string.Join(' ', arguments)} did not finish within {Timeout.TotalMinutes} minutes.");
        }

        process.WaitForExit();
        return (process.ExitCode, output.ToString(), error.ToString());
    }
}
