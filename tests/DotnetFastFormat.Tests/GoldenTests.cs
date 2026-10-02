using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

/// <summary>
/// Formats every <c>golden/**/*.in.cs</c> fixture, checks the invariants, and snapshots the output
/// with Verify. The formatter is the identity until milestone M1 lands the real one.
/// </summary>
public class GoldenTests
{
    private static readonly string Root = Path.Combine(SourceDirectory(), "golden");

    public static TheoryData<string> Fixtures()
    {
        var data = new TheoryData<string>();
        foreach (string path in Directory.EnumerateFiles(Root, "*.in.cs", SearchOption.AllDirectories).Order())
        {
            data.Add(Path.GetRelativePath(Root, path).Replace('\\', '/'));
        }

        return data;
    }

    [Fact]
    public void FindsFixtures() => Assert.NotEmpty(Fixtures());

    [Theory]
    [MemberData(nameof(Fixtures))]
    public Task Formats(string fixture)
    {
        string source = File.ReadAllText(Path.Combine(Root, fixture));
        string output = Invariants.FormatAndCheck(TestFormatters.Identity, source);
        string name = fixture[..^".in.cs".Length];
        return Verify(output, "cs")
            .UseDirectory(Path.GetDirectoryName(Path.Combine("golden", fixture))!)
            .UseFileName(Path.GetFileName(name));
    }

    private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
