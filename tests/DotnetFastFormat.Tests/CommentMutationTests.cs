namespace DotnetFastFormat.Tests;

/// <summary>Runs <see cref="CommentMutator"/> over this repository's own files.</summary>
public class CommentMutationTests
{
    public static TheoryData<string> Files() => RepositorySourceTests.Files();

    [Theory]
    [MemberData(nameof(Files))]
    public void FormattingMutatedFilesKeepsEveryInvariant(string file)
    {
        string source = File.ReadAllText(Path.Combine(RepositorySourceTests.RootDirectory, file));

        (string? first, int failures, int formatted) = CommentMutator.Run(source, perKind: 8, seed: file.Aggregate(20261005, (hash, c) => unchecked((hash * 31) + c)));

        Assert.True(failures == 0, $"{failures} mutations of {file} failed ({formatted} formatted):\n{first}");
    }
}
