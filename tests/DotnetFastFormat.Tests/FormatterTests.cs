using DotnetFastFormat.Core;

namespace DotnetFastFormat.Tests;

public class FormatterTests
{
    private static readonly RoslynFormatter Formatter = new();

    [Fact]
    public void UnsupportedNodesAreVerbatim()
    {
        const string Source = "class   E<[Foo] T>\n{\n  int   a ;\n}\n";

        Assert.Equal(Source, Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void AStatementWithACommentInsideIsVerbatimAndItsNeighboursAreFormatted()
    {
        const string Source = "class C\n{\n  void M()\n  {\n    int   a = 1;\n    int   b =   /* why */   2;\n    int   c = 3;\n  }\n}\n";

        Assert.Equal(
            "class C\n{\n    void M()\n    {\n        int a = 1;\n        int   b =   /* why */   2;\n        int c = 3;\n    }\n}\n",
            Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void AnExpressionWithNoPrinterIsVerbatimInsideAFormattedStatement()
    {
        const string Source = "class C\n{\n  void M()\n  {\n    var   q =   from   x in xs   select   x ;\n  }\n}\n";

        Assert.Equal(
            "class C\n{\n    void M()\n    {\n        var q = from   x in xs   select   x;\n    }\n}\n",
            Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void StatementsAreWrittenWithTheDominantLineEndingAndKeepTheLineEndingsInsideStrings()
    {
        const string Source = "class C\r\n{\r\n  void M()\r\n  {\r\n    if(a)b();\r\n    var s = @\"x\r\ny\";\r\n  }\r\n}\r\n";

        Assert.Equal(
            "class C\r\n{\r\n    void M()\r\n    {\r\n        if (a)\r\n            b();\r\n        var s = @\"x\r\ny\";\r\n    }\r\n}\r\n",
            Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void NodesWithCommentsInOtherPositionsAreVerbatim()
    {
        const string Source = "class   C\n{\n  void   M( int a /* first */ , int b ) { }\n  int   y ;\n}\n";

        Assert.Equal(
            "class C\n{\n    void   M( int a /* first */ , int b ) { }\n\n    int y;\n}\n",
            Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void CommentsAboveAndAfterNodesAreKept()
    {
        const string Source = "// about C\nclass   C   { }   // end\n";

        Assert.Equal("// about C\nclass C { } // end\n", Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void ParseErrorIsReported()
    {
        SyntaxErrorException error = Assert.Throws<SyntaxErrorException>(() => Formatter.Format("class C { void M( }"));

        Assert.Contains("CS", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("#if DEBUG\nclass   A { }\n#else\nclass   B { }\n#endif\n", "#if DEBUG\nclass   A { }\n#else\nclass B { }\n#endif\n")]
    [InlineData("#region r\nclass   A { }\n#endregion\n", "#region r\nclass A { }\n#endregion\n")]
    public void ConditionalDirectivesAreKept(string source, string expected) =>
        Assert.Equal(expected, Invariants.FormatAndCheck(Formatter, source));

    [Theory]
    [InlineData("// only a comment\n")]
    [InlineData("#nullable enable\n")]
    public void FilesWithoutDeclarationsAreVerbatim(string source) =>
        Assert.Equal(source, Invariants.FormatAndCheck(Formatter, source));

    [Fact]
    public void AFileWithoutDeclarationsLosesOnlyItsOuterWhitespace() =>
        Assert.Equal("// spaced   comment\n", Invariants.FormatAndCheck(Formatter, "   \n  // spaced   comment\n\n"));

    [Fact]
    public void AFileOfUsingsIsFormatted() =>
        Assert.Equal("using System;\n", Invariants.FormatAndCheck(Formatter, "using   System;\n"));

    [Fact]
    public void CommentsAtTheEndOfTheFileAreKept()
    {
        const string Source = "class   C { }\n\n\n// trailing\n#pragma warning restore CS0168";

        Assert.Equal("class C { }\n\n// trailing\n#pragma warning restore CS0168\n", Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void LineDirectivesThatRemapLinesKeepTheWholeFileAsWritten()
    {
        const string Source = "#line 100 \"x.cs\"\nclass   A { }\n";

        Assert.Equal(Source, Invariants.FormatAndCheck(Formatter, Source));
        Assert.Equal("#line hidden\nclass A { }\n", Invariants.FormatAndCheck(Formatter, "#line hidden\nclass   A { }\n"));
    }

    [Theory]
    [InlineData("class C { }", "class C { }\n")]
    [InlineData("class C { }\n\n\n", "class C { }\n")]
    [InlineData("class C { }\r\n\r\n", "class C { }\r\n")]
    [InlineData("class   E<[Foo] T>\r\n{\r\n}", "class   E<[Foo] T>\r\n{\r\n}\r\n")]
    [InlineData("class   E<[Foo] T>\n{\r\n}\n", "class   E<[Foo] T>\n{\r\n}\n")]
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

        Assert.Equal("using A;\n// note\nusing B;\nusing C;\n\nnamespace N { }\n", once);
    }

    [Fact]
    public void CommentsAndDirectivesKeepTheDominantLineEnding()
    {
        const string Source = "// head\r\nclass   A\r\n{\r\n    int   x ; // note\r\n#if DEBUG\r\n    int   y;\r\n#endif\r\n    /* a\r\n       b */\r\n    int z;\r\n    // last\r\n}\r\n";

        Assert.Equal(
            "// head\r\nclass A\r\n{\r\n    int x; // note\r\n#if DEBUG\r\n    int   y;\r\n#endif\r\n    /* a\r\n       b */\r\n    int z;\r\n    // last\r\n}\r\n",
            Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void AFileScopedNamespaceKeepsItsOwnAndItsLastMembersTrailingComments()
    {
        const string Source = "namespace N; // header\nclass   A { } // last\n";

        Assert.Equal("namespace N; // header\n\nclass A { } // last\n", Invariants.FormatAndCheck(Formatter, Source));
    }

    [Fact]
    public void ACommentBeforeTheClosingBraceOfAnEmptyBodyKeepsTheBodyOpen()
    {
        const string Source = "class A\n{\n// d\n}\n";

        Assert.Equal("class A\n{\n    // d\n}\n", Invariants.FormatAndCheck(Formatter, Source));
    }
}
