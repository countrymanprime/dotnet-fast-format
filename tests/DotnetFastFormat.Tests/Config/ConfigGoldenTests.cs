using DotnetFastFormat.Core;
using DotnetFastFormat.Core.Config;

namespace DotnetFastFormat.Tests.Config;

/// <summary>
/// Formats a fixture under an <c>.editorconfig</c>: <c>golden/config/NAME.in.cs</c> with
/// <c>golden/config/NAME.VARIANT.editorconfig</c> gives the snapshot <c>NAME.VARIANT.verified.cs</c>. The same input
/// under two or more configurations shows what each key changes (CFG-001, CFG-006).
/// </summary>
public class ConfigGoldenTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(SourceDirectory(), "..", "golden", "config"));

    public static TheoryData<string> Variants()
    {
        var data = new TheoryData<string>();
        foreach (string path in Directory.EnumerateFiles(Root, "*.editorconfig").Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(path)[..^".editorconfig".Length]);
        }

        return data;
    }

    [Fact]
    public void FindsVariants() => Assert.NotEmpty(Variants());

    [Theory]
    [MemberData(nameof(Variants))]
    public Task Formats(string variant)
    {
        string fixture = variant[..variant.IndexOf('.', StringComparison.Ordinal)];
        string config = File.ReadAllText(Path.Combine(Root, variant + ".editorconfig"));
        string source = File.ReadAllText(Path.Combine(Root, fixture + ".in.cs"));

        // The configuration lives in a virtual directory so no .editorconfig of this repository applies.
        string directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "fast-format-config-golden"));
        string configPath = Path.Combine(directory, ".editorconfig");
        var resolver = new EditorConfigResolver(path => string.Equals(path, configPath, StringComparison.Ordinal) ? config : null, directory);
        ResolvedSettings settings = resolver.Resolve(Path.Combine(directory, fixture + ".in.cs"));
        Assert.Empty(settings.Warnings);

        string output = Invariants.FormatAndCheck(new RoslynFormatter(), source, settings.Options);

        return Verify(output, "cs")
            .UseDirectory(Path.Combine("..", "golden", "config"))
            .UseFileName(variant);
    }

    private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
