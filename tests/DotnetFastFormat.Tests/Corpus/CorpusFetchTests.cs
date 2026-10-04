using System.Text.RegularExpressions;

using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests.Corpus;

/// <summary>
/// Fetches every pinned repository over the network and checks the checkout. Slow tier: it needs
/// network access and downloads tens of megabytes, so it runs in the scheduled workflow and on demand.
/// </summary>
[Trait("Category", "Slow")]
public sealed partial class CorpusFetchTests
{
    public static TheoryData<string> RepositoryNames()
    {
        var data = new TheoryData<string>();
        foreach (CorpusRepository repository in CorpusManifest.Load(CorpusManifest.DefaultPath()))
        {
            data.Add(repository.Name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RepositoryNames))]
    public void FetchesTheExactPinnedCommit(string name)
    {
        CorpusRepository repository = CorpusManifest.Load(CorpusManifest.DefaultPath()).Single(r => string.Equals(r.Name, name, StringComparison.Ordinal));

        FetchResult first = CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot());

        Assert.Equal(repository.Sha, GitRunner.RunChecked(first.Path, "rev-parse", "HEAD").Trim());
        Assert.True(
            Directory.EnumerateFiles(first.Path, "*.cs", SearchOption.AllDirectories).Count() >= 50,
            $"{name} should contain real C# source.");
        Assert.Contains(
            Directory.EnumerateFiles(first.Path).Select(Path.GetFileName),
            file => file is not null && LicenseFile().IsMatch(file));

        // Fetching again must find the same clean checkout and do nothing.
        Assert.True(CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot()).Reused);
    }

    [GeneratedRegex("^licen[cs]e", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex LicenseFile();
}
