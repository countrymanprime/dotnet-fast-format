using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests.Corpus;

/// <summary>Runs <see cref="CommentMutator"/> over every file of the pinned corpus. Slow tier: it needs the corpus fetched and formats tens of thousands of inputs.</summary>
[Trait("Category", "Slow")]
public class CorpusMutationTests
{
    public static TheoryData<string> RepositoryNames() => CorpusFetchTests.RepositoryNames();

    [Theory]
    [MemberData(nameof(RepositoryNames))]
    public void FormattingMutatedFilesKeepsEveryInvariant(string name)
    {
        CorpusRepository repository = CorpusManifest.Load(CorpusManifest.DefaultPath()).Single(r => string.Equals(r.Name, name, StringComparison.Ordinal));
        string root = CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot()).Path;
        var reports = new List<string>();
        int failures = 0;

        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string relative = Path.GetRelativePath(root, path);
            (string? first, int count, _) = CommentMutator.Run(File.ReadAllText(path), perKind: 3, seed: relative.Aggregate(7, (hash, c) => unchecked((hash * 31) + c)));
            failures += count;
            if (first is not null && reports.Count < 3)
            {
                reports.Add($"{relative}: {first}");
            }
        }

        Assert.True(failures == 0, $"{failures} mutations failed in {name}. First reports:\n{string.Join("\n=====\n", reports)}");
    }
}
