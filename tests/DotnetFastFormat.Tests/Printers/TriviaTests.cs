using DotnetFastFormat.Core;
using DotnetFastFormat.Core.Printers;
using Microsoft.CodeAnalysis;

namespace DotnetFastFormat.Tests.Printers;

public class TriviaTests
{
    [Fact]
    public void WhitespaceOnlyHasNoLines()
    {
        LeadingTrivia? trivia = Leading("\n\n   class A { }");

        Assert.NotNull(trivia);
        Assert.Empty(trivia.Lines);
        Assert.True(trivia.BlankLineAfter);
    }

    [Fact]
    public void OwnLineCommentsAreLinesWithBlankLineFlags()
    {
        LeadingTrivia trivia = Leading("// a\n\n// b\nclass A { }")!;

        Assert.Equal(2, trivia.Lines.Count);
        Assert.False(trivia.Lines[0].BlankLineBefore);
        Assert.True(trivia.Lines[1].BlankLineBefore);
        Assert.False(trivia.BlankLineAfter);
        Assert.All(trivia.Lines, line => Assert.Equal(TriviaLineKind.Comment, line.Kind));
    }

    [Fact]
    public void ADocumentationCommentIsOneLineEntryPerSourceLineWithoutItsIndentation()
    {
        LeadingTrivia trivia = Leading("    /// <summary>\n    ///   text\n    /// </summary>\n    class A { }")!;

        TriviaLine line = Assert.Single(trivia.Lines);
        Assert.Equal(["/// <summary>", "///   text", "/// </summary>"], line.Text);
    }

    [Fact]
    public void AMultiLineCommentKeepsItsInnerLines()
    {
        LeadingTrivia trivia = Leading("  /* a\n       b */\n  class A { }")!;

        TriviaLine line = Assert.Single(trivia.Lines);
        Assert.Equal(["/* a", "       b */"], line.Text);
    }

    [Fact]
    public void DirectivesAreClassifiedAndRegionsAreMarked()
    {
        LeadingTrivia trivia = Leading("#pragma warning disable CS0168\n#region R\nclass A { }")!;

        Assert.Equal([TriviaLineKind.Directive, TriviaLineKind.Region], trivia.Lines.Select(l => l.Kind));
        Assert.Equal("#pragma warning disable CS0168", trivia.Lines[0].Text[0]);
    }

    [Fact]
    public void DisabledTextIsKeptLineForLine()
    {
        LeadingTrivia trivia = Leading("#if X\n   int   odd;\n\n#endif\nclass A { }")!;

        Assert.Equal([TriviaLineKind.Directive, TriviaLineKind.DisabledText, TriviaLineKind.Directive], trivia.Lines.Select(l => l.Kind));
        Assert.Equal(["   int   odd;", string.Empty], trivia.Lines[1].Text);
    }

    [Theory]
    [InlineData("/* c */ class A { }")]
    [InlineData("/* a */ /* b */\nclass A { }")]
    public void ACommentFollowedByCodeOnItsLineIsNotClean(string source) =>
        Assert.Null(Leading(source));

    [Fact]
    public void ACommentAtTheEndOfTheFileWithoutANewlineIsClean()
    {
        SyntaxToken end = ((Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)SourceParser.Parse("class A { }\n// end").GetRoot(TestContext.Current.CancellationToken)).EndOfFileToken;

        LeadingTrivia? trivia = TriviaLines.ParseLeading(end.LeadingTrivia, atEndOfFile: true);

        Assert.NotNull(trivia);
        Assert.Single(trivia.Lines);
    }

    [Theory]
    [InlineData("int x; // note", "// note")]
    [InlineData("int x;   /* note */", "/* note */")]
    [InlineData("int x;", null)]
    public void ATrailingCommentIsAtMostOneSingleLineComment(string source, string? expected)
    {
        SyntaxToken semicolon = Tokens($"class A {{\n{source}\n}}").First(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SemicolonToken));

        (bool clean, string? comment) = TriviaLines.ParseTrailing(semicolon.TrailingTrivia);

        Assert.True(clean);
        Assert.Equal(expected, comment);
    }

    [Theory]
    [InlineData("int x; /* a */ // b")]
    [InlineData("int x; /* a\n b */")]
    [InlineData("int a; /* a */ int b;")]
    public void ShapesWithMoreThanOneCommentOrAMultiLineCommentAreNotClean(string source)
    {
        SyntaxToken semicolon = Tokens($"class A {{\n{source}\n}}").First(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SemicolonToken));

        Assert.False(TriviaLines.ParseTrailing(semicolon.TrailingTrivia).Clean);
    }

    private static LeadingTrivia? Leading(string source) =>
        TriviaLines.ParseLeading(Tokens(source).First().LeadingTrivia, atEndOfFile: false);

    private static IEnumerable<SyntaxToken> Tokens(string source) =>
        SourceParser.Parse(source).GetRoot(TestContext.Current.CancellationToken).DescendantTokens();
}
