using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

/// <summary>
/// Formats every <c>golden/**/*.in.cs</c> fixture, checks the invariants, and snapshots the output
/// with Verify. Everything is verbatim until the per-node builders land.
/// </summary>
public class GoldenTests
{
    private static readonly string Root = Path.Combine(SourceDirectory(), "golden");

    public static TheoryData<string> Fixtures()
    {
        var data = new TheoryData<string>();
        foreach (string path in Directory.EnumerateFiles(Root, "*.in.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
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
        string output = Invariants.FormatAndCheck(new RoslynFormatter(), source);
        string name = fixture[..^".in.cs".Length];
        return Verify(output, "cs")
            .UseDirectory(Path.GetDirectoryName(Path.Combine("golden", fixture))!)
            .UseFileName(Path.GetFileName(name));
    }

    private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
