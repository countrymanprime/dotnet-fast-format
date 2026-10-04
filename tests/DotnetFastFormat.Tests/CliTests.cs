using System.Diagnostics;
using DotnetFastFormat.Cli;

namespace DotnetFastFormat.Tests;

public class CliTests
{
    [Fact]
    public void HelpPrintsUsageAndExitsZero()
    {
        var (code, output, error) = Run("--help");

        Assert.Equal(0, code);
        Assert.Contains("C# formatter", output, StringComparison.Ordinal);
        Assert.Contains("Usage:", output, StringComparison.Ordinal);
        Assert.Equal(string.Empty, error);
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
        Assert.Equal(string.Empty, output);
        Assert.Contains("--no-such-option", error, StringComparison.Ordinal);
    }

    [Fact]
    public void NoPathsIsAnError()
    {
        var (code, output, error) = Run();

        Assert.Equal(2, code);
        Assert.Equal(string.Empty, output);
        Assert.Contains("No paths", error, StringComparison.Ordinal);
    }

    [Fact]
    public void DoubleDashLetsAPathStartWithADash()
    {
        var (code, _, error) = Run("--", "-odd.cs");

        Assert.Equal(2, code);
        Assert.Contains("-odd.cs", error, StringComparison.Ordinal);
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

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int code = CliApp.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    private static (int Code, string Out, string Err) RunProcess(string arg)
    {
        string dll = Path.Combine(AppContext.BaseDirectory, "dotnet-fast-format.dll");
        using Process process = Process.Start(new ProcessStartInfo("dotnet", [dll, arg])
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        })!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }
}
