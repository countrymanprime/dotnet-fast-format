using DotnetFastFormat.Core;

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

    [Theory]
    [MemberData(nameof(Files))]
    public void FormattingMutatedFilesKeepsEveryInvariantUnderOtherSettings(string file)
    {
        string source = File.ReadAllText(Path.Combine(RepositorySourceTests.RootDirectory, file));
        var settings = new FormatOptions
        {
            IndentStyle = IndentStyle.Tab,
            IndentSize = 4,
            TabWidth = 3,
            MaxLineLength = 70,
            EndOfLine = LineEnding.CrLf,
            InsertFinalNewline = false,
        };

        (string? first, int failures, int formatted) = CommentMutator.Run(source, perKind: 3, seed: file.Aggregate(20261006, (hash, c) => unchecked((hash * 31) + c)), settings);

        Assert.True(failures == 0, $"{failures} mutations of {file} failed ({formatted} formatted):\n{first}");
    }
}
