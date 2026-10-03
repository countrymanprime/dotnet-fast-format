namespace DotnetFastFormat.Tests.Corpus;

public class CorpusManifestTests
{
    private const string GoodSha = "4e13299d4b0ec96bd4df9954ef646bd2d1b5bf2a";

    [Fact]
    public void PinnedManifestIsValid()
    {
        IReadOnlyList<CorpusRepository> repositories = CorpusManifest.Load(CorpusManifest.DefaultPath());

        Assert.True(repositories.Count >= 5, "The corpus should hold several repositories.");
        Assert.Empty(CorpusManifest.Validate(repositories));
    }

    [Fact]
    public void AcceptsAWellFormedEntry() =>
        Assert.Empty(CorpusManifest.Validate([Repo("good-name", "https://github.com/o/r", GoodSha)]));

    [Theory]
    [InlineData("../evil", "https://github.com/o/r", GoodSha)]
    [InlineData("a/b", "https://github.com/o/r", GoodSha)]
    [InlineData("..", "https://github.com/o/r", GoodSha)]
    [InlineData("Upper", "https://github.com/o/r", GoodSha)]
    [InlineData("ok", "http://github.com/o/r", GoodSha)]
    [InlineData("ok", "https://example.com/o/r", GoodSha)]
    [InlineData("ok", "https://github.com/o/r/extra", GoodSha)]
    [InlineData("ok", "https://github.com/o/r", "main")]
    [InlineData("ok", "https://github.com/o/r", "4E13299D4B0EC96BD4DF9954EF646BD2D1B5BF2A")]
    [InlineData("ok", "https://github.com/o/r", "4e13299")]
    public void RejectsABadEntry(string name, string url, string sha) =>
        Assert.NotEmpty(CorpusManifest.Validate([Repo(name, url, sha)]));

    [Fact]
    public void RejectsDuplicateNamesAndEmptyLicense()
    {
        IReadOnlyList<string> errors = CorpusManifest.Validate(
        [
            Repo("same", "https://github.com/o/a", GoodSha),
            Repo("same", "https://github.com/o/b", GoodSha) with { License = string.Empty },
        ]);

        Assert.Contains(errors, e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("license", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsAnEmptyManifest() =>
        Assert.NotEmpty(CorpusManifest.Validate([]));

    [Fact]
    public void ParseRejectsUnknownProperties() =>
        Assert.ThrowsAny<Exception>(() => CorpusManifest.Parse("""{ "repositories": [], "surprise": 1 }"""));

    private static CorpusRepository Repo(string name, string url, string sha) =>
        new(name, url, "v1", sha, "MIT");
}
