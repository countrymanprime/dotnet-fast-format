using DotnetFastFormat.Core;
using DotnetFastFormat.Corpus;

namespace DotnetFastFormat.Tests.Corpus;

/// <summary>
/// Formats every C# file of each pinned repository and checks every invariant. Slow tier: it needs the corpus
/// fetched (network) and formats thousands of files. A file with syntax errors may be rejected; anything else
/// that throws, or breaks an invariant, is a failure.
/// </summary>
[Trait("Category", "Slow")]
public class CorpusFormatTests
{
    public static TheoryData<string> RepositoryNames() => CorpusFetchTests.RepositoryNames();

    [Theory]
    [MemberData(nameof(RepositoryNames))]
    public void FormattingKeepsEveryInvariant(string name)
    {
        CorpusRepository repository = CorpusManifest.Load(CorpusManifest.DefaultPath()).Single(r => string.Equals(r.Name, name, StringComparison.Ordinal));
        string root = CorpusFetcher.EnsureFetched(repository, CorpusFetcher.CorpusRoot()).Path;
        var formatter = new RoslynFormatter();
        var failures = new List<string>();
        int files = 0;
        int rejected = 0;

        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            files++;
            try
            {
                Invariants.FormatAndCheck(formatter, File.ReadAllText(path));
            }
            catch (FormatterException)
            {
                rejected++;
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetRelativePath(root, path)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.True(files > 0, $"{name} has no C# files.");
        Assert.True(failures.Count == 0, $"{failures.Count} of {files} files failed ({rejected} rejected):\n{string.Join('\n', failures.Take(25))}");
    }
}
