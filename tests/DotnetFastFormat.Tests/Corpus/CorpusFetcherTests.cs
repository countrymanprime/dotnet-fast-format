using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests.Corpus;

/// <summary>Exercises the fetch step offline, against a throwaway local git repository.</summary>
public sealed class CorpusFetcherTests : IDisposable
{
    private readonly string workspace = Path.Combine(Path.GetTempPath(), "dff-corpus-" + Guid.NewGuid().ToString("N"));

    public void Dispose() => DirectoryCleanup.Delete(workspace);

    [Fact]
    public void FetchesTheExactPinnedCommit()
    {
        (CorpusRepository repository, string root) = CreateSource();

        FetchResult result = CorpusFetcher.EnsureFetched(repository, root);

        Assert.False(result.Reused);
        Assert.Equal(repository.Sha, GitRunner.RunChecked(result.Path, "rev-parse", "HEAD").Trim());
        Assert.Equal("class A { }\n", File.ReadAllText(Path.Combine(result.Path, "A.cs")));
    }

    [Fact]
    public void ReusesAnUntouchedCheckout()
    {
        (CorpusRepository repository, string root) = CreateSource();
        CorpusFetcher.EnsureFetched(repository, root);

        Assert.True(CorpusFetcher.EnsureFetched(repository, root).Reused);
    }

    [Fact]
    public void RefetchesACheckoutThatWasModified()
    {
        (CorpusRepository repository, string root) = CreateSource();
        string path = CorpusFetcher.EnsureFetched(repository, root).Path;
        File.WriteAllText(Path.Combine(path, "A.cs"), "tampered");

        FetchResult result = CorpusFetcher.EnsureFetched(repository, root);

        Assert.False(result.Reused);
        Assert.Equal("class A { }\n", File.ReadAllText(Path.Combine(result.Path, "A.cs")));
    }

    [Fact]
    public void FailsWhenTheShaDoesNotExist()
    {
        (CorpusRepository repository, string root) = CreateSource();
        CorpusRepository missing = repository with { Sha = new string('a', 40) };

        Assert.Throws<InvalidOperationException>(() => CorpusFetcher.EnsureFetched(missing, root));
    }

    [Fact]
    public void RefusesToOverwriteADirectoryThatIsNotAGitCheckout()
    {
        (CorpusRepository repository, string root) = CreateSource();
        string occupied = Path.Combine(root, repository.Name);
        Directory.CreateDirectory(occupied);
        File.WriteAllText(Path.Combine(occupied, "keep.txt"), "mine");

        Assert.Throws<InvalidOperationException>(() => CorpusFetcher.EnsureFetched(repository, root));
        Assert.True(File.Exists(Path.Combine(occupied, "keep.txt")));
    }

    [Fact]
    public void RefusesANameThatEscapesTheCorpusRoot()
    {
        (CorpusRepository repository, string root) = CreateSource();

        Assert.Throws<InvalidOperationException>(() => CorpusFetcher.EnsureFetched(repository with { Name = "../escape" }, root));
    }

    private (CorpusRepository Repository, string Root) CreateSource()
    {
        string source = Path.Combine(workspace, "source");
        Directory.CreateDirectory(source);
        GitRunner.RunChecked(source, "init", "-q");
        File.WriteAllText(Path.Combine(source, "A.cs"), "class A { }\n");
        GitRunner.RunChecked(source, "add", "A.cs");
        GitRunner.RunChecked(
            source,
            "-c",
            "user.name=test",
            "-c",
            "user.email=test@example.com",
            "-c",
            "commit.gpgsign=false",
            "commit",
            "-q",
            "-m",
            "init");
        string sha = GitRunner.RunChecked(source, "rev-parse", "HEAD").Trim();

        var repository = new CorpusRepository("local-source", new Uri(source).AbsoluteUri, "v1", sha, "MIT");
        return (repository, Path.Combine(workspace, "corpus"));
    }
}
