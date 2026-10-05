using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

public class FormatterOptionsTests
{
    private const string Nested = "namespace N\n{\n  class A\n  {\n    int   x ;\n  }\n}\n";

    private static readonly RoslynFormatter Formatter = new();

    [Fact]
    public void TheDefaultOptionsAreTheDefaultStyle()
    {
        Assert.Equal(Formatter.Format(Nested), Formatter.Format(Nested, FormatOptions.Default));
        Assert.Equal("namespace N\n{\n    class A\n    {\n        int x;\n    }\n}\n", Formatter.Format(Nested));
    }

    [Fact]
    public void TabsIndentEachLevelWithOneTab() =>
        Assert.Equal(
            "namespace N\n{\n\tclass A\n\t{\n\t\tint x;\n\t}\n}\n",
            Invariants.FormatAndCheck(Formatter, Nested, new FormatOptions { IndentStyle = IndentStyle.Tab }));

    [Fact]
    public void TabsWithADifferentTabWidthFillWithSpaces() =>
        Assert.Equal(
            "namespace N\n{\n    class A\n    {\n\tint x;\n    }\n}\n",
            Invariants.FormatAndCheck(Formatter, Nested, new FormatOptions { IndentStyle = IndentStyle.Tab, IndentSize = 4, TabWidth = 8 }));

    [Fact]
    public void TheIndentSizeSetsTheSpacesPerLevel() =>
        Assert.Equal(
            "namespace N\n{\n  class A\n  {\n    int x;\n  }\n}\n",
            Invariants.FormatAndCheck(Formatter, Nested, new FormatOptions { IndentSize = 2 }));

    [Fact]
    public void MaxLineLengthSetsTheWidth()
    {
        const string Source = "class A\n{\n    void Method(int first, int second) { }\n}\n";
        string signature = "    void Method(int first, int second) { }";

        Assert.Equal(42, signature.Length);
        Assert.Contains(signature, Invariants.FormatAndCheck(Formatter, Source, new FormatOptions { MaxLineLength = 42 }), StringComparison.Ordinal);
        Assert.DoesNotContain(signature, Invariants.FormatAndCheck(Formatter, Source, new FormatOptions { MaxLineLength = 41 }), StringComparison.Ordinal);
        Assert.Contains(signature, Invariants.FormatAndCheck(Formatter, Source, new FormatOptions { MaxLineLength = null }), StringComparison.Ordinal);
    }

    [Fact]
    public void TabsCountAsTheTabWidthForTheLineWidth()
    {
        const string Source = "class A\n{\n    void Method(int first, int second) { }\n}\n";

        // One tab of width 4 is the same 4 columns as four spaces: 42 fits, 41 breaks.
        var tabs = new FormatOptions { IndentStyle = IndentStyle.Tab, IndentSize = 4, TabWidth = 4 };
        Assert.Contains("\tvoid Method(int first, int second) { }", Invariants.FormatAndCheck(Formatter, Source, tabs with { MaxLineLength = 42 }), StringComparison.Ordinal);
        Assert.DoesNotContain("\tvoid Method(int first, int second) { }", Invariants.FormatAndCheck(Formatter, Source, tabs with { MaxLineLength = 41 }), StringComparison.Ordinal);

        // A tab of width 8 makes the same line 46 columns.
        var wide = new FormatOptions { IndentStyle = IndentStyle.Tab, IndentSize = 8, TabWidth = 8 };
        Assert.Contains("\tvoid Method(int first, int second) { }", Invariants.FormatAndCheck(Formatter, Source, wide with { MaxLineLength = 46 }), StringComparison.Ordinal);
        Assert.DoesNotContain("\tvoid Method(int first, int second) { }", Invariants.FormatAndCheck(Formatter, Source, wide with { MaxLineLength = 45 }), StringComparison.Ordinal);
    }

    [Fact]
    public void CopiedTextIsNotReindentedUnderTabs()
    {
        const string Source = "class A\n{\n    void M()\n    {\n        int   y ;\n    }\n}\n";

        Assert.Equal(
            "class A\n{\n\tvoid M()\n\t{\n        int   y ;\n    }\n}\n",
            Invariants.FormatAndCheck(Formatter, Source, new FormatOptions { IndentStyle = IndentStyle.Tab }));
    }

    [Theory]
    [InlineData(LineEnding.Lf, "class A\n{\n    int x;\n}\n")]
    [InlineData(LineEnding.CrLf, "class A\r\n{\r\n    int x;\r\n}\r\n")]
    public void EndOfLineSetsTheTerminatorForMixedInput(LineEnding ending, string expected) =>
        Assert.Equal(expected, Invariants.FormatAndCheck(Formatter, "class   A\r\n{\r\n    int   x ;\n}\n", new FormatOptions { EndOfLine = ending }));

    [Fact]
    public void WithoutEndOfLineTheDominantTerminatorIsUsed() =>
        Assert.Equal("class A\r\n{\r\n    int x;\r\n}\r\n", Invariants.FormatAndCheck(Formatter, "class   A\r\n{\r\n    int   x ;\r\n}\n"));

    [Theory]
    [InlineData(LineEnding.Lf)]
    [InlineData(LineEnding.CrLf)]
    public void EndOfLineIsAppliedToCopiedTextCommentsAndDirectives(LineEnding ending)
    {
        const string Source = "// head\r\nenum E\n{\r\n  A,\n  B\r\n}\n/* a\r\n   b */\nclass A\r\n{\n#if X\r\n    int   y;\n#endif\r\n}\n";
        string newLine = ending == LineEnding.Lf ? "\n" : "\r\n";

        string output = Invariants.FormatAndCheck(Formatter, Source, new FormatOptions { EndOfLine = ending });

        Assert.Equal(
            string.Join(newLine, "// head", "enum E", "{", "  A,", "  B", "}", string.Empty, "/* a", "   b */", "class A", "{", "#if X", "    int   y;", "#endif", "}", string.Empty),
            output);
    }

    [Theory]
    [InlineData(LineEnding.Lf, "\r\n")]
    [InlineData(LineEnding.CrLf, "\n")]
    public void EndOfLineIsNeverAppliedInsideATokenButTheCodeAroundItIs(LineEnding ending, string insideTheString)
    {
        string source = "class   A { string s = @\"one" + insideTheString + "two\"; string t = \"\"\"\n  a" + insideTheString + "  b\n  \"\"\"; }\r\n";
        string newLine = ending == LineEnding.Lf ? "\n" : "\r\n";

        string output = Invariants.FormatAndCheck(Formatter, source, new FormatOptions { EndOfLine = ending });

        Assert.Contains("@\"one" + insideTheString + "two\"", output, StringComparison.Ordinal);
        Assert.Contains("  a" + insideTheString + "  b", output, StringComparison.Ordinal);
        Assert.StartsWith("class A" + newLine + "{" + newLine, output, StringComparison.Ordinal);
    }

    [Fact]
    public void EndOfLineIsAppliedToTheTextOutsideATokenWhenTheTokenIsPartOfACopiedNode()
    {
        // A field whose initializer holds a line break is copied as written; its own line breaks are in a token.
        const string Source = "class A\r\n{\r\n    string s = @\"a\r\nb\";\r\n    int   x ;\r\n}\r\n";

        string output = Invariants.FormatAndCheck(Formatter, Source, new FormatOptions { EndOfLine = LineEnding.Lf });

        Assert.Equal("class A\n{\n    string s = @\"a\r\nb\";\n    int x;\n}\n", output);
    }

    [Theory]
    [InlineData("class   A { }\n", "class A { }")]
    [InlineData("class   A { }", "class A { }")]
    [InlineData("class   A { }\r\n", "class A { }")]
    [InlineData("// only a comment\n", "// only a comment")]
    [InlineData("#nullable enable\n", "#nullable enable")]
    [InlineData("", "")]
    [InlineData("  \n", "")]
    public void InsertFinalNewlineFalseEndsWithoutALineBreak(string source, string expected) =>
        Assert.Equal(expected, Invariants.FormatAndCheck(Formatter, source, new FormatOptions { InsertFinalNewline = false }));

    [Theory]
    [InlineData("class   A { }", "class A { }\n")]
    [InlineData("class   A { }\n\n\n", "class A { }\n")]
    [InlineData("", "")]
    public void InsertFinalNewlineTrueEndsWithExactlyOneLineBreak(string source, string expected) =>
        Assert.Equal(expected, Invariants.FormatAndCheck(Formatter, source, new FormatOptions { InsertFinalNewline = true }));

    [Fact]
    public void NoFinalNewlineKeepsTheLineEndingOfTheRest() =>
        Assert.Equal(
            "class A\r\n{\r\n    int x;\r\n}",
            Invariants.FormatAndCheck(Formatter, "class   A\r\n{\r\n    int   x ;\r\n}\r\n", new FormatOptions { InsertFinalNewline = false }));

    [Fact]
    public void ACommentAtTheEndOfTheFileIsKeptWithoutAFinalNewline() =>
        Assert.Equal(
            "class A { }\n\n// trailing",
            Invariants.FormatAndCheck(Formatter, "class   A { }\n\n\n// trailing\n", new FormatOptions { InsertFinalNewline = false }));

    [Fact]
    public void ADirectiveAtTheEndOfTheFileIsKeptWithoutAFinalNewline() =>
        Assert.Equal(
            "class A { }\n#pragma warning restore CS0168",
            Invariants.FormatAndCheck(Formatter, "class   A { }\n#pragma warning restore CS0168\n", new FormatOptions { InsertFinalNewline = false }));

    [Fact]
    public void TheInterfaceOverloadWithoutOptionsPassesTheDefaults()
    {
        var recorder = new OptionsRecorder();

        _ = ((IFormatter)recorder).Format("class A { }");

        Assert.Same(FormatOptions.Default, recorder.Received);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(FormatOptions.MaxColumns + 1)]
    public void ColumnsOutsideTheRangeAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormatOptions { IndentSize = value });
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormatOptions { TabWidth = value });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(FormatOptions.MaxLineLengthLimit + 1)]
    public void ALineLengthOutsideTheRangeIsRejected(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormatOptions { MaxLineLength = value });

    [Fact]
    public void NullOptionsAreRejected() =>
        Assert.Throws<ArgumentNullException>(() => Formatter.Format("class A { }", null!));

    private sealed class OptionsRecorder : IFormatter
    {
        public FormatOptions? Received { get; private set; }

        public string Format(string source, FormatOptions options)
        {
            Received = options;
            return source;
        }
    }
}
