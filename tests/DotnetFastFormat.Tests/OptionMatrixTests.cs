using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

/// <summary>
/// Formats every golden input under each supported combination of settings and checks every invariant, so no
/// configuration can break idempotency, tree equivalence, comment preservation or validity (CFG-013).
/// </summary>
public class OptionMatrixTests
{
    private static readonly Dictionary<string, FormatOptions> Settings = new(StringComparer.Ordinal)
    {
        ["tabs"] = new FormatOptions { IndentStyle = IndentStyle.Tab },
        ["tabs-i4-w8"] = new FormatOptions { IndentStyle = IndentStyle.Tab, IndentSize = 4, TabWidth = 8 },
        ["tabs-i8-w3"] = new FormatOptions { IndentStyle = IndentStyle.Tab, IndentSize = 8, TabWidth = 3 },
        ["two-spaces"] = new FormatOptions { IndentSize = 2 },
        ["eight-spaces"] = new FormatOptions { IndentSize = 8 },
        ["width-40"] = new FormatOptions { MaxLineLength = 40 },
        ["width-60"] = new FormatOptions { MaxLineLength = 60 },
        ["width-120"] = new FormatOptions { MaxLineLength = 120 },
        ["width-off"] = new FormatOptions { MaxLineLength = null },
        ["lf"] = new FormatOptions { EndOfLine = LineEnding.Lf },
        ["crlf"] = new FormatOptions { EndOfLine = LineEnding.CrLf },
        ["no-final-newline"] = new FormatOptions { InsertFinalNewline = false },
        ["combined"] = new FormatOptions
        {
            IndentStyle = IndentStyle.Tab,
            IndentSize = 2,
            TabWidth = 2,
            MaxLineLength = 50,
            EndOfLine = LineEnding.CrLf,
            InsertFinalNewline = false,
        },
    };

    private static readonly string Root = Path.Combine(SourceDirectory(), "golden");

    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (string path in Directory.EnumerateFiles(Root, "*.in.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            string fixture = Path.GetRelativePath(Root, path).Replace('\\', '/');
            foreach (string name in Settings.Keys)
            {
                data.Add(name, fixture);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryInvariantHoldsUnder(string setting, string fixture)
    {
        string source = File.ReadAllText(Path.Combine(Root, fixture));

        Invariants.FormatAndCheck(new RoslynFormatter(), source, Settings[setting]);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryInvariantHoldsUnderWithCrlfInput(string setting, string fixture)
    {
        string source = File.ReadAllText(Path.Combine(Root, fixture)).Replace("\n", "\r\n", StringComparison.Ordinal);

        Invariants.FormatAndCheck(new RoslynFormatter(), source, Settings[setting]);
    }

    private static string SourceDirectory([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
