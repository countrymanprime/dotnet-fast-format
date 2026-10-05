using System.Globalization;
using DotnetFastFormat.Core;
using DotnetFastFormat.Core.Config;

namespace DotnetFastFormat.Tests.Config;

public class EditorConfigResolverTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "fast-format-resolver"));

    private readonly Dictionary<string, string> files = new(StringComparer.Ordinal);
    private readonly List<string> reads = [];
    private readonly HashSet<string> unreadable = new(StringComparer.Ordinal);

    private EditorConfigResolver? shared;

    [Fact]
    public void WithoutAnyConfigurationTheDefaultsApply()
    {
        ResolvedSettings settings = Resolve("a.cs");

        Assert.Equal(FormatOptions.Default, settings.Options);
        Assert.Empty(settings.Warnings);
    }

    [Fact]
    public void ResolutionOrder()
    {
        Config(".", "[*]\nindent_size = 2\nend_of_line = lf\nmax_line_length = 80\n");
        Config("sub", "[*.cs]\nindent_size = 8\nmax_line_length = 120\n");

        FormatOptions top = Resolve("top.cs").Options;
        FormatOptions nested = Resolve("sub/inner.cs").Options;

        Assert.Equal("Space 2 2 80 Lf", Describe(top));
        Assert.Equal("Space 8 8 120 Lf", Describe(nested));
    }

    [Fact]
    public void ALaterSectionOverridesAnEarlierOneInTheSameFile()
    {
        Config(".", "[*]\nindent_size = 2\n[*.cs]\nindent_size = 3\n[*.vb]\nindent_size = 9\n[a.cs]\nmax_line_length = 50\n");

        FormatOptions options = Resolve("a.cs").Options;

        Assert.Equal("Space 3 3 50 dominant", Describe(options));
    }

    [Fact]
    public void ALaterAssignmentOverridesAnEarlierOneInTheSameSection()
    {
        Config(".", "[*]\nindent_size = 2\nindent_size = 6\n");

        Assert.Equal(6, Resolve("a.cs").Options.IndentSize);
    }

    [Fact]
    public void StopsAtRoot()
    {
        Config(".", "[*]\nindent_size = 2\nend_of_line = crlf\n");
        Config("proj", "root = true\n[*]\nindent_style = tab\n");

        FormatOptions inside = Resolve("proj/a.cs").Options;

        Assert.Equal("Tab 4 4 100 dominant", Describe(inside));
        Assert.DoesNotContain(reads, path => string.Equals(path, ConfigPath("."), StringComparison.Ordinal));
        Assert.Equal("Space 2 2 100 CrLf", Describe(Resolve("b.cs").Options));
    }

    [Fact]
    public void WithoutRootTheSearchGoesUpToTheTopOfTheFileSystem()
    {
        Resolve("a.cs");

        Assert.Contains(reads, path => string.Equals(path, Path.Combine(Path.GetPathRoot(Root)!, ".editorconfig"), StringComparison.Ordinal));
    }

    [Fact]
    public void ACeilingStopsTheSearch()
    {
        Config(".", "[*]\nindent_size = 2\n");
        Config("repo", "[*]\nend_of_line = lf\n");
        var bounded = new EditorConfigResolver(Read, Path.Combine(Root, "repo"));

        FormatOptions options = bounded.Resolve(Path.Combine(Root, "repo", "src", "a.cs")).Options;

        Assert.Equal("Space 4 4 100 Lf", Describe(options));
        Assert.DoesNotContain(reads, path => string.Equals(path, ConfigPath("."), StringComparison.Ordinal));
    }

    [Fact]
    public void ASectionOnlyAppliesToAMatchingFile()
    {
        Config(".", "[*.vb]\nindent_size = 9\n[sub/*.cs]\nindent_size = 3\n[Program.cs]\nmax_line_length = 60\n");

        Assert.Equal(4, Resolve("a.cs").Options.IndentSize);
        Assert.Equal(3, Resolve("sub/a.cs").Options.IndentSize);
        Assert.Equal(4, Resolve("other/sub/a.cs").Options.IndentSize);
        Assert.Equal(60, Resolve("deep/er/Program.cs").Options.MaxLineLength);
        Assert.Equal(100, Resolve("Programs.cs").Options.MaxLineLength);
    }

    [Fact]
    public void ASectionWithAPathIsRelativeToTheDirectoryOfItsFile()
    {
        Config("repo", "[src/*.cs]\nindent_size = 2\n[/top.cs]\nindent_size = 3\n");

        Assert.Equal(2, Resolve("repo/src/a.cs").Options.IndentSize);
        Assert.Equal(4, Resolve("src/a.cs").Options.IndentSize);
        Assert.Equal(4, Resolve("repo/other/src/a.cs").Options.IndentSize);
        Assert.Equal(3, Resolve("repo/top.cs").Options.IndentSize);
        Assert.Equal(4, Resolve("repo/x/top.cs").Options.IndentSize);
    }

    [Fact]
    public void KeysAndValuesAreCaseInsensitive()
    {
        Config(".", "[*]\nIndent_Style = TAB\nEND_OF_LINE = CRLF\nInsert_Final_Newline = FALSE\nCharset = UTF-8-BOM\nMax_Line_Length = OFF\n");

        FormatOptions options = Resolve("a.cs").Options;

        Assert.Equal(IndentStyle.Tab, options.IndentStyle);
        Assert.Equal(LineEnding.CrLf, options.EndOfLine);
        Assert.False(options.InsertFinalNewline);
        Assert.Equal(ByteOrderMarkPolicy.Add, options.ByteOrderMark);
        Assert.Null(options.MaxLineLength);
    }

    [Fact]
    public void UnsetRemovesAValue()
    {
        Config(".", "[*]\nindent_size = 2\nmax_line_length = 60\nend_of_line = crlf\n");
        Config("sub", "[*]\nindent_size = unset\nmax_line_length = UNSET\n");
        Config("other", "[*]\nindent_size = unset\nindent_size = 5\n");

        FormatOptions unset = Resolve("sub/a.cs").Options;
        FormatOptions reset = Resolve("other/a.cs").Options;

        Assert.Equal("Space 4 4 100 CrLf", Describe(unset));
        Assert.Equal(5, reset.IndentSize);
    }

    [Fact]
    public void UnknownKeysAreIgnored()
    {
        Config(".", "[*]\ncsharp_new_line_before_open_brace = all\ndotnet_sort_system_directives_first = true\nfoo = bar\ntrim_trailing_whitespace = true\nspelling_language = en\nindent_size = 3\n");

        ResolvedSettings settings = Resolve("a.cs");

        Assert.Equal(3, settings.Options.IndentSize);
        Assert.Empty(settings.Warnings);
    }

    [Theory]
    [InlineData("indent_size = abc", "indent_size = abc is not a whole number from 1 to 256, or tab")]
    [InlineData("indent_size = 0", "indent_size = 0 is not a whole number")]
    [InlineData("indent_size = -2", "indent_size = -2 is not a whole number")]
    [InlineData("indent_size = 999", "indent_size = 999 is not a whole number")]
    [InlineData("indent_size = 99999999999", "indent_size = 99999999999 is not a whole number")]
    [InlineData("indent_size = 2.5", "indent_size = 2.5 is not a whole number")]
    [InlineData("indent_size =", "indent_size =  is not a whole number")]
    [InlineData("tab_width = 0", "tab_width = 0 is not a whole number from 1 to 256")]
    [InlineData("indent_style = tabs", "indent_style = tabs is not space or tab")]
    [InlineData("max_line_length = 0", "max_line_length = 0 is not a whole number from 1 to 1000000, or off")]
    [InlineData("max_line_length = wide", "max_line_length = wide is not a whole number")]
    [InlineData("end_of_line = cr", "end_of_line = cr is not supported (use lf or crlf)")]
    [InlineData("end_of_line = native", "end_of_line = native is not lf or crlf")]
    [InlineData("insert_final_newline = maybe", "insert_final_newline = maybe is not true or false")]
    [InlineData("charset = latin1", "charset = latin1 is not supported (files are read and written as UTF-8)")]
    [InlineData("charset = utf-16le", "charset = utf-16le is not supported")]
    [InlineData("charset = ebcdic", "charset = ebcdic is not utf-8 or utf-8-bom")]
    public void InvalidValuesWarn(string line, string expectedText)
    {
        Config(".", "[*]\n" + line + "\n");

        ResolvedSettings settings = Resolve("a.cs");

        string warning = Assert.Single(settings.Warnings);
        Assert.Contains(expectedText, warning, StringComparison.Ordinal);
        Assert.Contains(ConfigPath(".") + "(2): ", warning, StringComparison.Ordinal);
        Assert.EndsWith("; ignored", warning, StringComparison.Ordinal);
        Assert.Equal(FormatOptions.Default, settings.Options);
    }

    [Fact]
    public void AnInvalidValueFallsBackToAnEarlierValidOne()
    {
        Config(".", "[*]\nindent_size = 2\n");
        Config("sub", "[*]\nindent_size = lots\n");

        ResolvedSettings settings = Resolve("sub/a.cs");

        Assert.Equal(2, settings.Options.IndentSize);
        Assert.Contains("indent_size = lots", Assert.Single(settings.Warnings), StringComparison.Ordinal);
    }

    [Fact]
    public void AnInvalidValueInASectionThatDoesNotApplyIsNotReported()
    {
        Config(".", "[*.vb]\nindent_size = lots\n[*.cs]\nindent_size = 2\n");

        ResolvedSettings settings = Resolve("a.cs");

        Assert.Empty(settings.Warnings);
        Assert.Equal(2, settings.Options.IndentSize);
    }

    [Fact]
    public void LinesThatAreNotValidAreReported()
    {
        Config(".", "[*]\nnonsense\nindent_size = 2\n");

        ResolvedSettings settings = Resolve("a.cs");

        Assert.Equal(2, settings.Options.IndentSize);
        Assert.Contains(ConfigPath(".") + "(2): ", Assert.Single(settings.Warnings), StringComparison.Ordinal);
    }

    [Fact]
    public void ASectionNameThatCannotBeMatchedIsReportedAndSkipped()
    {
        Config(".", "[" + string.Concat(Enumerable.Repeat("{a,b}", 11)) + "]\nindent_size = 9\n[*]\nindent_size = 2\n");

        ResolvedSettings settings = Resolve("a.cs");

        Assert.Equal(2, settings.Options.IndentSize);
        Assert.Contains("too long or too complex", Assert.Single(settings.Warnings), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "Space", 4, 4)]
    [InlineData("indent_style = tab", "Tab", 4, 4)]
    [InlineData("indent_style = tab\nindent_size = 2", "Tab", 2, 2)]
    [InlineData("indent_style = tab\ntab_width = 8", "Tab", 8, 8)]
    [InlineData("indent_style = tab\nindent_size = tab", "Tab", 4, 4)]
    [InlineData("indent_style = tab\nindent_size = tab\ntab_width = 3", "Tab", 3, 3)]
    [InlineData("indent_style = tab\nindent_size = 4\ntab_width = 8", "Tab", 4, 8)]
    [InlineData("indent_style = tab\nindent_size = 8\ntab_width = 3", "Tab", 8, 3)]
    [InlineData("indent_style = space\nindent_size = 2", "Space", 2, 2)]
    [InlineData("indent_size = 6", "Space", 6, 6)]
    [InlineData("indent_size = tab", "Space", 4, 4)]
    [InlineData("tab_width = 8", "Space", 4, 8)]
    [InlineData("indent_style = tab\nindent_size = unset", "Tab", 4, 4)]
    public void IndentSizeAndTabWidthFollowTheSpecification(string lines, string style, int size, int tabWidth)
    {
        Config(".", "[*]\n" + lines + "\n");

        FormatOptions options = Resolve("a.cs").Options;

        Assert.Equal($"{style} {size} {tabWidth}", $"{options.IndentStyle} {options.IndentSize} {options.TabWidth}");
    }

    [Theory]
    [InlineData("charset = utf-8", ByteOrderMarkPolicy.Remove)]
    [InlineData("charset = utf-8-bom", ByteOrderMarkPolicy.Add)]
    [InlineData("charset = unset", ByteOrderMarkPolicy.Preserve)]
    [InlineData("", ByteOrderMarkPolicy.Preserve)]
    public void CharsetSetsTheByteOrderMarkPolicy(string line, ByteOrderMarkPolicy expected)
    {
        Config(".", "[*]\n" + line + "\n");

        Assert.Equal(expected, Resolve("a.cs").Options.ByteOrderMark);
    }

    [Theory]
    [InlineData("max_line_length = 80", 80)]
    [InlineData("max_line_length = 1000000", 1_000_000)]
    [InlineData("max_line_length = off", null)]
    [InlineData("max_line_length = unset", 100)]
    public void MaxLineLengthIsANumberOrOff(string line, int? expected)
    {
        Config(".", "[*]\n" + line + "\n");

        Assert.Equal(expected, Resolve("a.cs").Options.MaxLineLength);
    }

    [Theory]
    [InlineData("insert_final_newline = false", false)]
    [InlineData("insert_final_newline = true", true)]
    [InlineData("insert_final_newline = unset", true)]
    public void InsertFinalNewlineIsATrueOrFalse(string line, bool expected)
    {
        Config(".", "[*]\n" + line + "\n");

        Assert.Equal(expected, Resolve("a.cs").Options.InsertFinalNewline);
    }

    [Fact]
    public void AnUnreadableFileIsSkippedWithAWarning()
    {
        Config(".", "[*]\nindent_size = 2\n");
        Config("sub", "[*]\nindent_size = 8\n");
        unreadable.Add(ConfigPath("sub"));

        ResolvedSettings settings = Resolve("sub/a.cs");

        Assert.Equal(2, settings.Options.IndentSize);
        string warning = Assert.Single(settings.Warnings);
        Assert.Contains(ConfigPath("sub"), warning, StringComparison.Ordinal);
        Assert.Contains("could not be read", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnreadableFileDoesNotStopTheSearch()
    {
        Config(".", "[*]\nend_of_line = crlf\n");
        unreadable.Add(ConfigPath("a/b"));

        ResolvedSettings settings = Resolve("a/b/c/x.cs");

        Assert.Equal(LineEnding.CrLf, settings.Options.EndOfLine);
        Assert.Single(settings.Warnings);
    }

    [Fact]
    public void EachFileIsReadOnce()
    {
        Config(".", "[*]\nindent_size = 2\n");
        Config("d3", "[*]\nindent_size = 3\n");
        for (int i = 0; i < 1000; i++)
        {
            Resolve($"d{i % 10}/e{i % 7}/f{i}.cs");
        }

        Assert.NotEmpty(reads);
        Assert.Equal(reads.Count, reads.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheSameDirectoryIsAnsweredFromTheCache()
    {
        Resolve("x/a.cs");
        int before = reads.Count;

        Resolve("x/b.cs");
        Resolve("x/y/c.cs");

        // Only x/y is new; its parents are known.
        Assert.Equal(before + 1, reads.Count);
    }

    [Fact]
    public void TheWarningsOfAFileComeWithEveryFileItAppliesTo()
    {
        Config(".", "[*]\nindent_size = lots\n");

        Assert.Single(Resolve("a.cs").Warnings);
        Assert.Single(Resolve("sub/b.cs").Warnings);
    }

    [Fact]
    public void ResolvingFromSeveralThreadsGivesTheSameAnswers()
    {
        Config(".", "[*]\nindent_size = 2\n");
        Config("sub", "[*.cs]\nindent_size = 3\n");
        var concurrent = new EditorConfigResolver(Read);

        int[] sizes = [.. Enumerable.Range(0, 200).AsParallel().AsOrdered().Select(i => concurrent.Resolve(Path.Combine(Root, i % 2 == 0 ? "sub" : "other", $"f{i}.cs")).Options.IndentSize)];

        Assert.Equal(Enumerable.Range(0, 200).Select(i => i % 2 == 0 ? 3 : 2), sizes);
        Assert.Equal(reads.Count, reads.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ANullPathIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new EditorConfigResolver(Read).Resolve(null!));

    private static string ConfigPath(string relativeDirectory) => Path.GetFullPath(Path.Combine(Root, relativeDirectory, ".editorconfig"));

    private static string Describe(FormatOptions options) =>
        $"{options.IndentStyle} {options.IndentSize} {options.TabWidth} {options.MaxLineLength?.ToString(CultureInfo.InvariantCulture) ?? "off"} {options.EndOfLine?.ToString() ?? "dominant"}";

    private string? Read(string path)
    {
        lock (reads)
        {
            reads.Add(path);
        }

        if (unreadable.Contains(path))
        {
            throw new IOException("denied");
        }

        return files.GetValueOrDefault(path);
    }

    private void Config(string relativeDirectory, string text) => files[ConfigPath(relativeDirectory)] = text;

    private ResolvedSettings Resolve(string relativePath)
    {
        shared ??= new EditorConfigResolver(Read);
        return shared.Resolve(Path.Combine(Root, relativePath));
    }
}
