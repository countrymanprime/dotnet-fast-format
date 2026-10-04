using DotnetFastFormat.Benchmarks;
using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests.Benchmarks;

public class WorkspacePathsTests
{
    [Fact]
    public void WorkingCopiesLiveOutsideTheRepositoryTree()
    {
        // MSBuild, .editorconfig and global.json all search upward, so a working copy inside this repository
        // would pick up this repository's Directory.Build.props, Directory.Packages.props and .editorconfig.
        string work = Path.GetFullPath(WorkspacePaths.WorkRoot(overridePath: null));
        string repository = Path.GetFullPath(RepositoryRoot.Find());

        Assert.False(
            work.StartsWith(repository + Path.DirectorySeparatorChar, StringComparison.Ordinal),
            $"{work} is inside {repository}.");
    }

    [Fact]
    public void AnOverrideWins() =>
        Assert.Equal(Path.GetFullPath("custom-dir"), WorkspacePaths.WorkRoot("custom-dir"));
}
