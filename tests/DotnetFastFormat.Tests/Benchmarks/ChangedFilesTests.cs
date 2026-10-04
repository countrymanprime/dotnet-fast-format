using DotnetFastFormat.Benchmarks;

namespace DotnetFastFormat.Tests.Benchmarks;

public class ChangedFilesTests
{
    [Fact]
    public void CountsModifiedFiles() =>
        Assert.Equal(2, ChangedFiles.Count(" M src/A.cs\n M src/B.cs\n"));

    [Fact]
    public void IgnoresTheGlobalJsonTheHarnessRemoves() =>
        Assert.Equal(1, ChangedFiles.Count(" M src/A.cs\n D global.json\n D sub/global.json\n"));

    [Fact]
    public void NoOutputMeansNoChanges() =>
        Assert.Equal(0, ChangedFiles.Count(string.Empty));
}
