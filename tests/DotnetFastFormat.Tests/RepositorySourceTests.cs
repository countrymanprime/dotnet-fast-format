using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

/// <summary>Formats this repository's own C# files and checks every invariant, a cheap property test on real code.</summary>
public class RepositorySourceTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(SourceDirectory(), "..", ".."));

    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (string directory in new[] { "src", "tests", "benchmarks" })
        {
            foreach (string path in Directory.EnumerateFiles(Path.Combine(Root, directory), "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !p.Contains($"{Path.DirectorySeparatorChar}golden{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal))
            {
                data.Add(Path.GetRelativePath(Root, path).Replace('\\', '/'));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void FormattingKeepsEveryInvariant(string file) =>
        Invariants.FormatAndCheck(new RoslynFormatter(), File.ReadAllText(Path.Combine(Root, file)));

    private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
