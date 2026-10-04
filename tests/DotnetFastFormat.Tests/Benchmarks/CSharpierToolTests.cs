using DotnetFastFormat.Benchmarks;

namespace DotnetFastFormat.Tests.Benchmarks;

public class CSharpierToolTests
{
    [Fact]
    public void ExitCodeZeroIsSuccess() =>
        Assert.True(CSharpierTool.IsSuccessfulRun(new ProcessOutcome(0, 1.0, "Formatted 10 files in 900ms.")));

    [Fact]
    public void ExitCodeOneAfterFormattingIsSuccessBecauseSomeInputsHaveSyntaxErrors() =>
        Assert.True(CSharpierTool.IsSuccessfulRun(new ProcessOutcome(1, 1.0, "(1,1): error CS8999: x | Formatted 422 files in 2950ms.")));

    [Fact]
    public void ExitCodeOneWithoutFormattingIsAFailure() =>
        Assert.False(CSharpierTool.IsSuccessfulRun(new ProcessOutcome(1, 1.0, "Error: could not find a project")));

    [Fact]
    public void OtherExitCodesAreFailures() =>
        Assert.False(CSharpierTool.IsSuccessfulRun(new ProcessOutcome(2, 1.0, "Formatted 3 files in 5ms.")));
}
