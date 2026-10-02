using System.Diagnostics;
using DotnetFastFormat.Cli;

namespace DotnetFastFormat.Tests;

public class CliTests
{
    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int code = CliApp.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void HelpPrintsUsageAndExitsZero()
    {
        var (code, output, error) = Run("--help");

        Assert.Equal(0, code);
        Assert.Contains("C# formatter", output, StringComparison.Ordinal);
        Assert.Contains("Usage:", output, StringComparison.Ordinal);
        Assert.Equal("", error);
    }

    [Fact]
    public void VersionExitsZero()
    {
        var (code, output, _) = Run("--version");

        Assert.Equal(0, code);
        Assert.NotEmpty(output.Trim());
    }

    [Fact]
    public void UnknownOptionExitsTwoAndWritesNothingToStdout()
    {
        var (code, output, error) = Run("--no-such-option");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("--no-such-option", error, StringComparison.Ordinal);
    }

    [Fact]
    public void NotYetImplementedExitsTwo()
    {
        var (code, output, error) = Run("some/file.cs");

        Assert.Equal(2, code);
        Assert.Equal("", output);
        Assert.Contains("not implemented", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DoubleDashLetsAPathStartWithADash()
    {
        var (code, _, error) = Run("--", "-odd.cs");

        Assert.Equal(2, code);
        Assert.Contains("not implemented", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProcessExitCodeAndCommandNameMatchTheContract()
    {
        var (badCode, _, _) = RunProcess("--no-such-option");
        var (helpCode, helpOutput, _) = RunProcess("--help");

        Assert.Equal(2, badCode);
        Assert.Equal(0, helpCode);
        Assert.Contains("dotnet-fast-format", helpOutput, StringComparison.Ordinal);
    }

    private static (int Code, string Out, string Err) RunProcess(string arg)
    {
        string dll = Path.Combine(AppContext.BaseDirectory, "dotnet-fast-format.dll");
        using Process process = Process.Start(new ProcessStartInfo("dotnet", [dll, arg])
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        })!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }
}
