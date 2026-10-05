using DotnetFastFormat.Core.Layout;

namespace DotnetFastFormat.Tests.Layout;

public class DocPrinterTests
{
    [Fact]
    public void EmptyDocPrintsNothing() =>
        Assert.Equal(string.Empty, Print(Docs.Empty));

    [Fact]
    public void TextIsPrintedAsIs() =>
        Assert.Equal("hello", Print(Docs.Text("hello")));

    [Fact]
    public void ConcatPrintsItsPartsInOrder() =>
        Assert.Equal("abc", Print(Docs.Concat(Docs.Text("a"), Docs.Text("b"), Docs.Text("c"))));

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    public void TextContainingALineBreakIsRejected(string value) =>
        Assert.Throws<ArgumentException>(() => Docs.Text(value));

    [Fact]
    public void AGroupThatFitsStaysFlat() =>
        Assert.Equal("aa bb", Print(Words("aa", "bb"), width: 10));

    [Fact]
    public void AGroupThatDoesNotFitBreaksItsLines() =>
        Assert.Equal("aa\nbb", Print(Words("aa", "bb"), width: 4));

    [Fact]
    public void ALineExactlyTheWidthFitsAndOneMoreBreaks()
    {
        Assert.Equal("aaaaa bbbb", Print(Words("aaaaa", "bbbb"), width: 10));
        Assert.Equal("aaaaa\nbbbb", Print(Words("aaaaa", "bbbb"), width: 9));
    }

    [Fact]
    public void ASoftLineIsEmptyWhenFlatAndANewlineWhenBroken()
    {
        Doc call = Docs.Group(Docs.Concat(Docs.Text("f("), Docs.SoftLine, Docs.Text("x"), Docs.SoftLine, Docs.Text(")")));

        Assert.Equal("f(x)", Print(call, width: 20));
        Assert.Equal("f(\nx\n)", Print(call, width: 3));
    }

    [Fact]
    public void IndentAppliesAfterANewline() =>
        Assert.Equal(
            "a\n    b\nc",
            Print(Docs.Concat(Docs.Text("a"), Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("b"))), Docs.HardLine, Docs.Text("c"))));

    [Fact]
    public void TheIndentSizeIsConfigurable() =>
        Assert.Equal(
            "a\n  b",
            Print(Docs.Concat(Docs.Text("a"), Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("b")))), indentSize: 2));

    [Fact]
    public void IndentsNest() =>
        Assert.Equal(
            "a\n    b\n        c",
            Print(Docs.Concat(
                Docs.Text("a"),
                Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("b"), Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("c"))))))));

    [Fact]
    public void AHardLineBreaksEveryEnclosingGroupEvenWhenItAllFits()
    {
        Doc doc = Docs.Group(Docs.Concat(Docs.Text("a"), Docs.Line, Docs.Text("b"), Docs.HardLine, Docs.Text("c")));

        Assert.Equal("a\nb\nc", Print(doc, width: 100));
    }

    [Fact]
    public void AnInnerGroupStaysFlatWhenItFitsAndTheOuterOneBreaks()
    {
        Doc doc = Docs.Group(Docs.Concat(Docs.Text("outer-start"), Docs.Line, Docs.Group(Docs.Concat(Docs.Text("x"), Docs.Line, Docs.Text("y")))));

        Assert.Equal("outer-start\nx y", Print(doc, width: 12));
    }

    [Fact]
    public void TheOutermostGroupBreaksBeforeAnInnerOne()
    {
        Doc doc = Docs.Group(Docs.Concat(
            Docs.Text("call("),
            Docs.Indent(Docs.Concat(Docs.SoftLine, Docs.Group(Docs.Concat(Docs.Text("one,"), Docs.Line, Docs.Text("two"))))),
            Docs.SoftLine,
            Docs.Text(")")));

        Assert.Equal("call(one, two)", Print(doc, width: 14));
        Assert.Equal("call(\n    one, two\n)", Print(doc, width: 12));
    }

    [Fact]
    public void ATextLongerThanTheWidthIsStillPrintedWhole() =>
        Assert.Equal("a\nlooooooooooong", Print(Words("a", "looooooooooong"), width: 5));

    [Fact]
    public void FillPacksAsManyItemsOnALineAsFit() =>
        Assert.Equal("aaa bbb\nccc ddd", Print(Docs.Fill(FillParts("aaa", "bbb", "ccc", "ddd")), width: 8));

    [Fact]
    public void FillBreaksEverySeparatorWhenNothingFitsTogether() =>
        Assert.Equal("aaa\nbbb\nccc", Print(Docs.Fill(FillParts("aaa", "bbb", "ccc")), width: 3));

    [Fact]
    public void FillKeepsEverythingOnOneLineWhenItAllFits() =>
        Assert.Equal("aaa bbb ccc", Print(Docs.Fill(FillParts("aaa", "bbb", "ccc")), width: 40));

    [Fact]
    public void FillHandlesZeroOneAndTwoParts()
    {
        Assert.Equal(string.Empty, Print(Docs.Fill([])));
        Assert.Equal("aaa", Print(Docs.Fill([Docs.Text("aaa")])));
        Assert.Equal("aaa\nbbb", Print(Docs.Fill([Docs.Text("aaa"), Docs.Line, Docs.Text("bbb")]), width: 4));
    }

    [Fact]
    public void VerbatimTextIsEmittedUnchanged() =>
        Assert.Equal("a\n  b\n", Print(Docs.Verbatim("a\n  b\n")));

    [Fact]
    public void TrailingWhitespaceIsTrimmedBeforeALineBreakExceptInVerbatimText()
    {
        Assert.Equal("x\ny", Print(Docs.Concat(Docs.Text("x  "), Docs.HardLine, Docs.Text("y"))));
        Assert.Equal("x  \ny", Print(Docs.Concat(Docs.Verbatim("x  "), Docs.HardLine, Docs.Text("y"))));
    }

    [Fact]
    public void VerbatimTextWithALineBreakBreaksTheEnclosingGroup()
    {
        Doc doc = Docs.Group(Docs.Concat(Docs.Text("a"), Docs.Line, Docs.Verbatim("b\nc")));

        Assert.Equal("a\nb\nc", Print(doc, width: 100));
    }

    [Fact]
    public void AStringLiteralWithALineBreakOnlyCountsUpToTheBreakAndKeepsItsGroupFlat()
    {
        Doc doc = Docs.Group(Docs.Concat(Docs.Text("f("), Docs.SoftLine, Docs.Verbatim("@\"a\nb\"", forcesBreak: false), Docs.SoftLine, Docs.Text(")")));

        Assert.False(doc.ForcesBreak);
        Assert.Equal("f(@\"a\nb\")", Print(doc, width: 10));
        Assert.Equal("f(\n@\"a\nb\"\n)", Print(doc, width: 4));
    }

    [Fact]
    public void WhatFollowsAStringLiteralWithALineBreakIsMeasuredFromItsLastLine()
    {
        Doc doc = Docs.Group(Docs.Concat(Docs.Verbatim("a\nbbbb", forcesBreak: false), Docs.Line, Docs.Text("cc")));

        Assert.Equal("a\nbbbb cc", Print(doc, width: 8));
        Assert.Equal("a\nbbbb\ncc", Print(doc, width: 6));
    }

    [Fact]
    public void SingleLineVerbatimTextCountsTowardsTheWidth()
    {
        Doc doc = Docs.Group(Docs.Concat(Docs.Text("aa"), Docs.Line, Docs.Verbatim("bbbb")));

        Assert.Equal("aa bbbb", Print(doc, width: 7));
        Assert.Equal("aa\nbbbb", Print(doc, width: 6));
    }

    [Fact]
    public void TheConfiguredNewLineIsUsedForLineBreaks() =>
        Assert.Equal("a\r\nb", Print(Docs.Concat(Docs.Text("a"), Docs.HardLine, Docs.Text("b")), newLine: "\r\n"));

    [Fact]
    public void JoinPutsTheSeparatorBetweenItems() =>
        Assert.Equal("a, b, c", Print(Docs.Join(Docs.Text(", "), [Docs.Text("a"), Docs.Text("b"), Docs.Text("c")])));

    [Fact]
    public void ADocumentTenThousandGroupsDeepDoesNotOverflowTheStack()
    {
        Doc doc = Docs.Text("x");
        for (int i = 0; i < 10_000; i++)
        {
            doc = Docs.Group(Docs.Indent(Docs.Concat(Docs.Text("("), Docs.SoftLine, doc, Docs.SoftLine, Docs.Text(")"))));
        }

        string output = Print(doc, width: 100, indentSize: 0);

        Assert.Equal(10_000, output.Count(c => c == '('));
        Assert.Equal(10_000, output.Count(c => c == ')'));
    }

    [Fact]
    public void AHardLineTenThousandGroupsDeepStillBreaksEveryEnclosingGroup()
    {
        Doc doc = Docs.Concat(Docs.Text("a"), Docs.HardLine, Docs.Text("b"));
        for (int i = 0; i < 10_000; i++)
        {
            doc = Docs.Group(Docs.Concat(Docs.Text("["), Docs.Line, doc, Docs.Line, Docs.Text("]")));
        }

        string output = Print(doc, width: 1_000_000, indentSize: 0);

        Assert.StartsWith("[\n[\n", output, StringComparison.Ordinal);
        Assert.Equal(10_000, output.Count(c => c == ']'));
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(-1, 4)]
    [InlineData(80, -1)]
    public void InvalidOptionsAreRejected(int width, int indentSize) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocPrintOptions(width, indentSize));

    [Fact]
    public void IfBreakPrintsTheFlatContentsWhenTheGroupFits() =>
        Assert.Equal("aa bb {}", Print(Docs.Group(Docs.Concat(Words("aa", "bb"), Docs.IfBreak(Docs.Text("!"), Docs.Text(" {}")))), width: 20));

    [Fact]
    public void IfBreakPrintsTheBreakContentsWhenTheGroupBreaks() =>
        Assert.Equal("aa\nbb!", Print(Docs.Group(Docs.Concat(Words("aa", "bb"), Docs.IfBreak(Docs.Text("!"), Docs.Text(" {}")))), width: 4));

    [Fact]
    public void AColumnZeroDocStartsAtTheLeftMarginAndTheNextLineReturnsToTheIndent() =>
        Assert.Equal(
            "a\n    b\n#if X\n    c",
            Print(Docs.Indent(Docs.Concat(
                Docs.Text("a"),
                Docs.HardLine,
                Docs.Text("b"),
                Docs.HardLine,
                Docs.ColumnZero(Docs.Text("#if X")),
                Docs.HardLine,
                Docs.Text("c")))));

    [Fact]
    public void LinesInsideAColumnZeroDocAreNotIndented() =>
        Assert.Equal(
            "x\n    y\nz\nw",
            Print(Docs.Concat(
                Docs.Text("x"),
                Docs.Indent(Docs.Concat(
                    Docs.HardLine,
                    Docs.Text("y"),
                    Docs.HardLine,
                    Docs.ColumnZero(Docs.Concat(Docs.Text("z"), Docs.HardLine, Docs.Text("w"))))))));

    [Fact]
    public void AForcedGroupBreaksEvenWhenItFits() =>
        Assert.Equal("a\nb", Print(Docs.Group(Docs.Concat(Docs.Text("a"), Docs.Line, Docs.Text("b")), forceBreak: true), width: 100));

    [Fact]
    public void AForcedGroupBreaksItsEnclosingGroups()
    {
        Doc inner = Docs.Group(Docs.Concat(Docs.Text("x"), Docs.Line, Docs.Text("y")), forceBreak: true);

        Assert.Equal("a\nx\ny", Print(Docs.Group(Docs.Concat(Docs.Text("a"), Docs.Line, inner)), width: 100));
    }

    [Fact]
    public void AConditionalGroupPrintsTheFirstLayoutThatFits()
    {
        Doc doc = Docs.Conditional(Docs.Text("one line"), Docs.Text("second"), Docs.Text("last"));

        Assert.Equal("one line", Print(doc, width: 8));
        Assert.Equal("second", Print(doc, width: 7));
    }

    [Fact]
    public void AConditionalGroupPrintsItsLastLayoutBrokenWhenNoneFits()
    {
        Doc doc = Docs.Conditional(Docs.Text("long text here"), Docs.Group(Docs.Concat(Docs.Text("a"), Docs.Line, Docs.Text("b"))));

        Assert.Equal("a\nb", Print(doc, width: 2));
    }

    [Fact]
    public void ALayoutWithAForcedBreakFitsWhenTheTextBeforeTheBreakFits()
    {
        // f(x => { ... }): the opening line fits, so the layout that opens the last argument is chosen.
        Doc hugged = Docs.Concat(Docs.Text("f(x =>"), Docs.HardLine, Docs.Text("{ }"), Docs.Text(")"));
        Doc broken = Docs.Group(Docs.Concat(Docs.Text("f("), Docs.Indent(Docs.Concat(Docs.SoftLine, Docs.Text("x => { }"))), Docs.SoftLine, Docs.Text(")")));
        Doc doc = Docs.Conditional(hugged, broken);

        Assert.Equal("f(x =>\n{ })", Print(doc, width: 6));
        Assert.Equal("f(\n    x => { }\n)", Print(doc, width: 5));
    }

    [Fact]
    public void AConditionalGroupDoesNotBreakItsEnclosingGroupButWillBreakSaysSo()
    {
        Doc conditional = Docs.Conditional(Docs.Concat(Docs.Text("x"), Docs.HardLine, Docs.Text("y")), Docs.Text("z"));
        Doc group = Docs.Group(Docs.Concat(Docs.Text("a"), Docs.Line, conditional));

        Assert.False(conditional.ForcesBreak);
        Assert.True(conditional.WillBreak);
        Assert.Equal("a x\ny", Print(group, width: 100));
    }

    [Fact]
    public void TenThousandNestedConditionalGroupsDoNotOverflowTheStack()
    {
        Doc doc = Docs.Text("x");
        for (int i = 0; i < 10_000; i++)
        {
            doc = Docs.Conditional(Docs.Concat(Docs.Text("("), doc, Docs.Text(")")), Docs.Text("?"));
        }

        string output = Print(doc, width: 100_000, indentSize: 0);

        Assert.Equal(10_000, output.Count(c => c == '('));
    }

    public void TabIndentationWritesOneTabPerLevelWhenTheTabWidthIsTheIndentSize()
    {
        Doc doc = Docs.Concat(
            Docs.Text("a"),
            Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("b"), Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("c"))))));

        Assert.Equal("a\n\tb\n\t\tc", PrintWithTabs(doc, indentSize: 4, tabWidth: 4));
    }

    [Theory]
    [InlineData(4, 3, "a\n\t b\n\t\t  c")]
    [InlineData(8, 4, "a\n\t\tb\n\t\t\t\tc")]
    [InlineData(2, 4, "a\n  b\n\tc")]
    public void TabIndentationFillsWithTabsAndThenSpaces(int indentSize, int tabWidth, string expected)
    {
        Doc doc = Docs.Concat(
            Docs.Text("a"),
            Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("b"), Docs.Indent(Docs.Concat(Docs.HardLine, Docs.Text("c"))))));

        Assert.Equal(expected, PrintWithTabs(doc, indentSize, tabWidth));
    }

    [Fact]
    public void ATabCountsAsTheTabWidthWhenFitting()
    {
        Doc doc = Docs.Concat(Docs.Text("a"), Docs.Indent(Docs.Concat(Docs.HardLine, Words("aaaa", "bbbb"))));

        // The indent is one tab of 4 columns, so the 9-column group ends at column 13.
        Assert.Equal("a\n\taaaa bbbb", PrintWithTabs(doc, indentSize: 4, tabWidth: 4, width: 13));
        Assert.Equal("a\n\taaaa\n\tbbbb", PrintWithTabs(doc, indentSize: 4, tabWidth: 4, width: 12));
    }

    [Fact]
    public void ATabCountsAsTheConfiguredTabWidth()
    {
        Doc doc = Docs.Concat(Docs.Text("a"), Docs.Indent(Docs.Concat(Docs.HardLine, Words("aaaa", "bbbb"))));

        Assert.Equal("a\n\taaaa bbbb", PrintWithTabs(doc, indentSize: 8, tabWidth: 8, width: 17));
        Assert.Equal("a\n\taaaa\n\tbbbb", PrintWithTabs(doc, indentSize: 8, tabWidth: 8, width: 16));
    }

    [Fact]
    public void AColumnZeroDocAfterTabIndentationStartsAtColumnZero()
    {
        Doc doc = Docs.Concat(
            Docs.Text("x"),
            Docs.Indent(Docs.Concat(
                Docs.HardLine,
                Docs.ColumnZero(Docs.Concat(Docs.Text("ab "), Words("cc", "dd"))),
                Docs.HardLine,
                Docs.Text("y"))));

        // "ab cc dd" is exactly 8 columns; it only fits if trimming the tab put the column back to 0.
        Assert.Equal("x\nab cc dd\n\ty", PrintWithTabs(doc, indentSize: 4, tabWidth: 4, width: 8));
    }

    [Fact]
    public void TrailingIndentationIsRemovedBeforeALineBreakWithTabsToo() =>
        Assert.Equal(
            "a\n\n\tb",
            PrintWithTabs(
                Docs.Concat(Docs.Text("a"), Docs.Indent(Docs.Concat(Docs.HardLine, Docs.HardLine, Docs.Text("b")))),
                indentSize: 4,
                tabWidth: 4));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ATabWidthBelowOneIsRejected(int tabWidth) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocPrintOptions(tabWidth: tabWidth));

    private static string PrintWithTabs(Doc doc, int indentSize, int tabWidth, int width = 20) =>
        DocPrinter.Print(doc, new DocPrintOptions(width, indentSize, "\n", useTabs: true, tabWidth: tabWidth));

    private static string Print(Doc doc, int width = 20, int indentSize = 4, string newLine = "\n") =>
        DocPrinter.Print(doc, new DocPrintOptions(width, indentSize, newLine));

    private static Doc Words(params string[] words) =>
        Docs.Group(Docs.Join(Docs.Line, words.Select(Docs.Text)));

    private static Doc[] FillParts(params string[] words)
    {
        var parts = new List<Doc>();
        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                parts.Add(Docs.Line);
            }

            parts.Add(Docs.Text(words[i]));
        }

        return [.. parts];
    }
}
