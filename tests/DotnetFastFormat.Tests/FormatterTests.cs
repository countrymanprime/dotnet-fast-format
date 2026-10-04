using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

public class FormatterTests
{
    private static readonly RoslynFormatter Formatter = new();

    [Fact]
    public void UnsupportedNodesAreVerbatim()
    {
        const string Source = "class C\n{\n  void   M( )  {  }\n}\n";

        Assert.Equal(Source, Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void NodesWithCommentsAreVerbatim()
    {
        const string Source = "class C\n{\n  int   x ; // keep   this\n  /* a\n     b */ int y;\n}\n";

        Assert.Equal(Source, Invariants.FormatAndCheck(Formatter, Source));
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
    [InlineData("class C\r\n{\r\n}", "class C\r\n{\r\n}\r\n")]
    [InlineData("class C\n{\r\n}\n", "class C\n{\r\n}\n")]
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

        Assert.Equal(source + "\n", Formatter.Format(source));
    }
}
