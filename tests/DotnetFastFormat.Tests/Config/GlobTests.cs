using System.Diagnostics;
using DotnetFastFormat.Core.Config;

namespace DotnetFastFormat.Tests.Config;

/// <summary>
/// Section-name matching as the EditorConfig specification (https://spec.editorconfig.org/, "Glob Expressions")
/// defines it. A path is the file's path relative to the directory of the <c>.editorconfig</c>, with <c>/</c> separators.
/// </summary>
public class GlobTests
{
    [Theory]
    [InlineData("*", "a.cs", true)]
    [InlineData("*", "dir/a.cs", true)]
    [InlineData("*.cs", "a.cs", true)]
    [InlineData("*.cs", "a/b/c.cs", true)]
    [InlineData("*.cs", ".cs", true)]
    [InlineData("*.cs", "a.cs.txt", false)]
    [InlineData("*.cs", "a.vb", false)]
    [InlineData("*.CS", "a.cs", false)]
    [InlineData("a*e.c", "ae.c", true)]
    [InlineData("a*e.c", "abcde.c", true)]
    [InlineData("a*e.c", "a/e.c", false)]
    [InlineData("Bar/*", "Bar/foo.txt", true)]
    [InlineData("Bar/*", "Bar/foo/baz.txt", false)]
    [InlineData("Bar/*", "x/Bar/foo.txt", false)]
    [InlineData("Bar/**", "Bar/foo/baz.txt", true)]
    [InlineData("Bar/**", "Bar/foo.txt", true)]
    [InlineData("a**z.c", "amnz.c", true)]
    [InlineData("a**z.c", "a/z.c", true)]
    [InlineData("a**z.c", "am/nz.c", true)]
    [InlineData("a**z.c", "a/mnz.c", true)]
    [InlineData("**.cs", "a/b.cs", true)]
    [InlineData("**.cs", "b.cs", true)]
    [InlineData("src/**/*.cs", "src/a/b.cs", true)]
    [InlineData("src/**/*.cs", "src/a/b/c.cs", true)]
    [InlineData("src/**/*.cs", "src/a.cs", false)]
    [InlineData("src/**/*.cs", "x/src/a/b.cs", false)]
    [InlineData("src/**", "src/a.cs", true)]
    [InlineData("src/**", "other/a.cs", false)]
    public void StarsMatchWithinOrAcrossSeparators(string pattern, string path, bool expected) =>
        AssertMatch(pattern, path, expected);

    [Theory]
    [InlineData("som?.c", "some.c", true)]
    [InlineData("som?.c", "som.c", false)]
    [InlineData("som?.c", "som/.c", false)]
    [InlineData("som?.c", "somex.c", false)]
    [InlineData("a?", "ab", true)]
    [InlineData("?", "a", true)]
    [InlineData("?", "ab", false)]
    public void AQuestionMarkMatchesOneCharacterThatIsNotASeparator(string pattern, string path, bool expected) =>
        AssertMatch(pattern, path, expected);

    [Theory]
    [InlineData("[ab].a", "a.a", true)]
    [InlineData("[ab].a", "b.a", true)]
    [InlineData("[ab].a", "c.a", false)]
    [InlineData("[ab].a", "ab.a", false)]
    [InlineData("[!ab].a", "c.a", true)]
    [InlineData("[!ab].a", "a.a", false)]
    [InlineData("[!ab].a", "/.a", false)]
    [InlineData("[a-d].a", "c.a", true)]
    [InlineData("[a-d].a", "e.a", false)]
    [InlineData("[!a-d].a", "e.a", true)]
    [InlineData("[ab*c{1..2}]", "a", true)]
    [InlineData("[ab*c{1..2}]", "b", true)]
    [InlineData("[ab*c{1..2}]", "*", true)]
    [InlineData("[ab*c{1..2}]", "c", true)]
    [InlineData("[ab*c{1..2}]", "{", true)]
    [InlineData("[ab*c{1..2}]", "}", true)]
    [InlineData("[ab*c{1..2}]", "1", true)]
    [InlineData("[ab*c{1..2}]", ".", true)]
    [InlineData("[ab*c{1..2}]", "2", true)]
    [InlineData("[ab*c{1..2}]", "ab", false)]
    [InlineData("[ab*c{1..2}]", "x", false)]
    [InlineData("x[\\]]y", "x]y", true)]
    [InlineData("a[b.c", "a[b.c", true)]
    [InlineData("a[]b", "a[]b", true)]
    [InlineData("[a-]", "-", true)]
    [InlineData("[a-]", "a", true)]
    public void BracketsMatchOneCharacterOfASetAndEverythingInsideIsLiteral(string pattern, string path, bool expected) =>
        AssertMatch(pattern, path, expected);

    [Theory]
    [InlineData("*.{py,js}", "a.py", true)]
    [InlineData("*.{py,js}", "a.js", true)]
    [InlineData("*.{py,js}", "a.rb", false)]
    [InlineData("*.{cs,vb}", "x.vb", true)]
    [InlineData("{*.cs,*.vb}", "a.vb", true)]
    [InlineData("{single}.py", "{single}.py", true)]
    [InlineData("{single}.py", "single.py", false)]
    [InlineData("{a,b}{c,d}", "ac", true)]
    [InlineData("{a,b}{c,d}", "ad", true)]
    [InlineData("{a,b}{c,d}", "bd", true)]
    [InlineData("{a,b}{c,d}", "ab", false)]
    [InlineData("{a,b{c,d}}.x", "a.x", true)]
    [InlineData("{a,b{c,d}}.x", "bc.x", true)]
    [InlineData("{a,b{c,d}}.x", "bd.x", true)]
    [InlineData("{a,b{c,d}}.x", "b.x", false)]
    [InlineData("{,a}.py", ".py", true)]
    [InlineData("{,a}.py", "a.py", true)]
    [InlineData("{,a}.py", "b.py", false)]
    [InlineData("a{b,c.x", "a{b,c.x", true)]
    [InlineData("a{b,c.x", "ab", false)]
    [InlineData("a}b", "a}b", true)]
    [InlineData("{a\\,b,c}", "a,b", true)]
    [InlineData("{a\\,b,c}", "c", true)]
    [InlineData("{a\\,b,c}", "a", false)]
    [InlineData("{a,[b,]c}", "a", true)]
    [InlineData("{a,[b,]c}", ",c", true)]
    [InlineData("{a,b}/c", "a/c", true)]
    [InlineData("{a,b}/c", "b/c", true)]
    [InlineData("{a,b}/c", "x/a/c", false)]
    [InlineData("{a,b/c}.cs", "a.cs", true)]
    [InlineData("{a,b/c}.cs", "b/c.cs", true)]
    [InlineData("{a,b/c}.cs", "x/a.cs", false)]
    public void BracesMatchAnyOfTheirAlternativesAndASingleOneIsLiteral(string pattern, string path, bool expected) =>
        AssertMatch(pattern, path, expected);

    [Theory]
    [InlineData("{1..3}", "1", true)]
    [InlineData("{1..3}", "2", true)]
    [InlineData("{1..3}", "3", true)]
    [InlineData("{1..3}", "0", false)]
    [InlineData("{1..3}", "4", false)]
    [InlineData("{1..3}", "01", false)]
    [InlineData("{1..3}", "", false)]
    [InlineData("{3..120}", "42", true)]
    [InlineData("{3..120}", "120", true)]
    [InlineData("{3..120}", "121", false)]
    [InlineData("{3..120}", "2", false)]
    [InlineData("{-3..3}", "-3", true)]
    [InlineData("{-3..3}", "0", true)]
    [InlineData("{-3..3}", "-4", false)]
    [InlineData("{-3..3}", "4", false)]
    [InlineData("{-3..3}", "-", false)]
    [InlineData("file{10..12}.txt", "file11.txt", true)]
    [InlineData("file{10..12}.txt", "file9.txt", false)]
    [InlineData("file{10..12}.txt", "file13.txt", false)]
    [InlineData("file{1..12}.txt", "file12.txt", true)]
    [InlineData("file{1..12}.txt", "file2.txt", true)]
    [InlineData("{3..1}", "{3..1}", true)]
    [InlineData("{3..1}", "2", false)]
    [InlineData("{-4..-5}", "-4", false)]
    [InlineData("{1..2,5}", "1..2", true)]
    [InlineData("{1..2,5}", "5", true)]
    [InlineData("{1..2,5}", "1", false)]
    [InlineData("{a..b}", "{a..b}", true)]
    [InlineData("{1..99999999999999999999}", "{1..99999999999999999999}", true)]
    public void NumericRangesMatchIntegersAndAnythingElseInBracesIsLiteral(string pattern, string path, bool expected) =>
        AssertMatch(pattern, path, expected);

    [Theory]
    [InlineData("a\\*b", "a*b", true)]
    [InlineData("a\\*b", "axb", false)]
    [InlineData("a\\?b", "a?b", true)]
    [InlineData("a\\?b", "axb", false)]
    [InlineData("\\[a]", "[a]", true)]
    [InlineData("\\{a,b\\}", "{a,b}", true)]
    [InlineData("a\\\\b", "a\\b", true)]
    [InlineData("a\\", "a\\", true)]
    public void ABackslashEscapesTheNextCharacter(string pattern, string path, bool expected) =>
        AssertMatch(pattern, path, expected);

    [Theory]
    [InlineData("x.c", "x.c", true)]
    [InlineData("x.c", "d/x.c", true)]
    [InlineData("x.c", "d/e/x.c", true)]
    [InlineData("x.c", "dx.c", false)]
    [InlineData("/x.c", "x.c", true)]
    [InlineData("/x.c", "d/x.c", false)]
    [InlineData("d/*.c", "d/x.c", true)]
    [InlineData("d/*.c", "e/d/x.c", false)]
    [InlineData("d/*.c", "d/e/x.c", false)]
    [InlineData("/d/*.c", "d/x.c", true)]
    [InlineData("/d/*.c", "e/d/x.c", false)]
    [InlineData("dir/[ab]/x", "dir/a/x", true)]
    [InlineData("dir/[ab]/x", "dir/c/x", false)]
    [InlineData("[a/b]", "a", true)]
    [InlineData("[a/b]", "x/a", true)]
    [InlineData("d/", "d/x.c", false)]
    [InlineData("d/", "d", false)]
    [InlineData("**/x.c", "a/x.c", true)]
    [InlineData("**/x.c", "x.c", false)]
    public void AGlobWithASeparatorIsRelativeToTheConfigurationDirectoryAndOtherwiseMatchesAtAnyDepth(string pattern, string path, bool expected) =>
        AssertMatch(pattern, path, expected);

    [Fact]
    public void ARepeatedStarPatternFinishesQuicklyOnInputThatDoesNotMatch()
    {
        Glob glob = Glob.Parse("*a*a*a*a*a*a*a*a*a*a*a*a*b")!;
        string path = new('a', 200);
        var watch = Stopwatch.StartNew();

        bool matched = glob.IsMatch(path);

        Assert.False(matched);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"took {watch.Elapsed}");
    }

    [Fact]
    public void RepeatedNumericRangesFinishQuicklyToo()
    {
        Glob glob = Glob.Parse("{1..99999}{1..99999}{1..99999}{1..99999}{1..99999}{1..99999}x")!;
        var watch = Stopwatch.StartNew();

        bool matched = glob.IsMatch(new string('1', 60));

        Assert.False(matched);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"took {watch.Elapsed}");
    }

    [Fact]
    public void AnAlternationThatExpandsToTooManyPatternsIsRejected()
    {
        Assert.NotNull(Glob.Parse(string.Concat(Enumerable.Repeat("{a,b}", 10))));
        Assert.Null(Glob.Parse(string.Concat(Enumerable.Repeat("{a,b}", 11))));
    }

    [Fact]
    public void ADeepPatternDoesNotOverflowTheStack()
    {
        string deep = string.Concat(Enumerable.Repeat("{x,", 100)) + "a" + new string('}', 100);

        Assert.Null(Glob.Parse(deep));
        Assert.Null(Glob.Parse(new string('a', GlobParser.MaxLength + 1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("{,}")]
    [InlineData("{..}")]
    [InlineData("[!]")]
    [InlineData("[-]")]
    [InlineData("**")]
    [InlineData("***")]
    [InlineData("\\")]
    [InlineData("{{{{")]
    [InlineData("[[[[")]
    public void OddPatternsNeverThrow(string pattern)
    {
        Glob? glob = Glob.Parse(pattern);

        _ = glob?.IsMatch("a/b.cs");
        _ = glob?.IsMatch(string.Empty);
        _ = glob?.IsMatch(pattern);
    }

    [Fact]
    public void RandomPatternsNeverThrow()
    {
        var random = new Random(4242);
        const string Alphabet = "[]{}*?!,.-\\/ab19";
        for (int i = 0; i < 3000; i++)
        {
            string pattern = new([.. Enumerable.Range(0, random.Next(0, 14)).Select(_ => Alphabet[random.Next(Alphabet.Length)])]);
            string path = new([.. Enumerable.Range(0, random.Next(0, 12)).Select(_ => Alphabet[random.Next(Alphabet.Length)])]);

            _ = Glob.Parse(pattern)?.IsMatch(path);
        }
    }

    private static void AssertMatch(string pattern, string path, bool expected)
    {
        Glob? glob = Glob.Parse(pattern);

        Assert.NotNull(glob);
        Assert.Equal(expected, glob.IsMatch(path));
    }
}
