using DotnetFastFormat.Benchmarks;
using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests.Benchmarks;

public sealed class SolutionFinderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "dff-sln-" + Guid.NewGuid().ToString("N"));

    public SolutionFinderTests() => Directory.CreateDirectory(root);

    public void Dispose() => DirectoryCleanup.Delete(root);

    [Fact]
    public void PrefersARootSlnxOverOtherFiles()
    {
        Touch("Thing.slnx");
        Touch("Thing.sln.old");
        Touch("Thing.sln.DotSettings");
        Touch("src/Other.sln");

        Assert.Equal(Path.Combine(root, "Thing.slnx"), SolutionFinder.Find(root));
    }

    [Fact]
    public void FallsBackToANestedSolution()
    {
        Touch("src/Nested.sln");

        Assert.Equal(Path.Combine(root, "src", "Nested.sln"), SolutionFinder.Find(root));
    }

    [Fact]
    public void PrefersTheShallowestSolution()
    {
        Touch("a/b/Deep.sln");
        Touch("a/Shallow.sln");

        Assert.Equal(Path.Combine(root, "a", "Shallow.sln"), SolutionFinder.Find(root));
    }

    [Fact]
    public void ReturnsNullWhenThereIsNoSolution()
    {
        Touch("README.md");
        Touch("x.sln.old");

        Assert.Null(SolutionFinder.Find(root));
    }

    private void Touch(string relativePath)
    {
        string path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
    }
}
