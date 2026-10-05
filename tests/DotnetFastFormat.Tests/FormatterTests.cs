using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

public class FormatterTests
{
    private static readonly RoslynFormatter Formatter = new();

    [Fact]
    public void UnsupportedNodesAreVerbatim()
    {
        const string Source = "enum   E\n{\n  A ,  B\n}\n";

        Assert.Equal(Source, Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void NodesWithCommentsAreVerbatim()
    {
        const string Source = "class   C\n{\n  int   x ; // keep   this\n  /* a\n     b */ int y;\n}\n";
        const string Commented = "// about C\nclass   C   { }\n";

        Assert.Equal("class C\n{\n    int   x ; // keep   this\n    /* a\n     b */ int y;\n}\n", Invariants.FormatAndCheck(Formatter, Source));
        Assert.Equal(Commented, Invariants.FormatAndCheck(Formatter, Commented));
    }

    [Fact]
    public void ParseErrorIsReported()
    {
        SyntaxErrorException error = Assert.Throws<SyntaxErrorException>(() => Formatter.Format("class C { void M( }"));

        Assert.Contains("CS", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("#if DEBUG\nclass   A { }\n#else\nclass   B { }\n#endif\n")]
    [InlineData("#region r\nclass   A { }\n#endregion\n")]
    public void FilesWithConditionalDirectivesAreVerbatim(string source) =>
        Assert.Equal(source, Invariants.FormatAndCheck(Formatter, source));

    [Theory]
    [InlineData("// only a comment\n")]
    [InlineData("using   System;\n")]
    public void FilesWithoutDeclarationsAreVerbatim(string source) =>
        Assert.Equal(source, Invariants.FormatAndCheck(Formatter, source));

    [Fact]
    public void FileEndingInACommentIsVerbatim()
    {
        const string Source = "class   C { }\n// trailing\n";

        Assert.Equal(Source, Invariants.FormatAndCheck(Formatter, Source));
    }

    [Theory]
    [InlineData("class C { }", "class C { }\n")]
    [InlineData("class C { }\n\n\n", "class C { }\n")]
    [InlineData("class C { }\r\n\r\n", "class C { }\r\n")]
    [InlineData("enum E\r\n{\r\n}", "enum E\r\n{\r\n}\r\n")]
    [InlineData("enum E\n{\r\n}\n", "enum E\n{\r\n}\n")]
    [InlineData("", "")]
    public void EndsWithExactlyOneNewlineInTheDominantLineEnding(string source, string expected) =>
        Assert.Equal(expected, Formatter.Format(source));

    [Fact]
    public void DeeplyNestedInputIsRejectedWithoutCrashing()
    {
        string source = "class C { int x = " + new string('(', 5000) + "1" + new string(')', 5000) + "; }";
        Exception? thrown = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    Formatter.Format(source);
                }
                catch (FormatterException ex)
                {
                    thrown = ex;
                }
            },
            1024 * 1024);
        thread.Start();
        thread.Join();

        Assert.IsType<FormatterException>(thrown);
    }

    [Fact]
    public void NestingJustUnderTheLimitIsFormatted()
    {
        string source = "class C { int x = " + new string('(', 900) + "1" + new string(')', 900) + "; }";

        Assert.Contains(source[10..^2], Formatter.Format(source), StringComparison.Ordinal);
    }

    [Fact]
    public void UsesTheDominantLineEndingForTheLinesItWrites()
    {
        const string Source = "using   System;\r\nnamespace N\r\n{\r\n    class A { }\r\n}";

        Assert.Equal("using System;\r\n\r\nnamespace N\r\n{\r\n    class A { }\r\n}\r\n", Formatter.Format(Source));
    }

    [Fact]
    public void FormattedAndVerbatimSiblingsAreStable()
    {
        const string Source = "using   A;\n// note\nusing   B;\nusing   C;\n\nnamespace N { }\n";

        string once = Invariants.FormatAndCheck(Formatter, Source);

        Assert.Equal("using A;\n// note\nusing   B;\nusing C;\n\nnamespace N { }\n", once);
    }
}
